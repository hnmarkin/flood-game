from fastapi import FastAPI
from pydantic import BaseModel
from typing import Optional
import os
from google import genai
import json
import re
import traceback

# 🔑 1) Put your Gemini API key here
GEMINI_API_KEY = "AIzaSyADplNe3fUVewWNVkPbUU88L9L5FESHIVs"
gemini_client = genai.Client(api_key=GEMINI_API_KEY)


app = FastAPI()

# ---------- Data models from Unity ----------

class Persona(BaseModel):
    personaName: str
    personaType: str
    tone: str
    urgency: str
    empathy: str
    harshness: int
    description: str

class EventRequest(BaseModel):
    persona: Persona
    eventText: str

class EventResult(BaseModel):
    action: str
    commentary: str


def build_event_prompt(req: EventRequest) -> str:
    p = req.persona

    persona_block = f"""
You are playing a role in a flood-resilience city game called SurgeCity.

Persona:
- Name: {p.personaName}
- Type: {p.personaType}
- Tone: {p.tone}
- Urgency: {p.urgency}
- Empathy: {p.empathy}
- Harshness Level (0-100): {p.harshness}

Persona Description:
{p.description}
"""

    task_block = f"""
Game Event:
\"\"\"{req.eventText}\"\"\"

Task:
Respond in character as this persona.

Return ONLY valid JSON in this format:
{{
  "action": "short emergency instruction or decision",
  "commentary": "1–2 emotionally realistic sentences from this persona's perspective"
}}
"""

    return persona_block + "\n" + task_block


def call_gemini_for_event(req: EventRequest) -> EventResult:
    prompt = build_event_prompt(req)

    print("\n================= PROMPT SENT TO GEMINI =================")
    print(prompt)
    print("=========================================================\n")

    try:
        # 💡 Ask Gemini to respond as JSON ONLY
        resp = gemini_client.models.generate_content(
            model="gemini-2.0-flash",
            contents=prompt,
            config={
                "response_mime_type": "application/json"
            }
        )

        text = resp.text or ""
        print("----- RAW GEMINI RESPONSE (should be JSON) -----")
        print(text)
        print("------------------------------------------------")

        # Now text SHOULD already be clean JSON
        data = json.loads(text)

        return EventResult(
            action=data.get("action", "No action"),
            commentary=data.get("commentary", "No commentary")
        )

    except Exception as e:
        import traceback
        print("❌ ERROR while calling Gemini or parsing JSON:")
        traceback.print_exc()

        # Safe fallback
        return EventResult(
            action="Use rule-based evacuation",
            commentary="The AI failed to respond correctly. Falling back to a simple safety rule."
        )


# ---------- API endpoints ----------

@app.get("/")
def root():
    return {"message": "LLM server running with Gemini Flash 2.0"}

@app.post("/event", response_model=EventResult)
def event_response(req: EventRequest):
    print(">>> /event called with:", req)
    result = call_gemini_for_event(req)
    print("<<< Returning EventResult:", result)
    return result
