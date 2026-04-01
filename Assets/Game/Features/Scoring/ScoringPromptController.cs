// /*HAS NOT BEEN MODIFIED WITH THE DIFFERENT METRICS OF SUCCESS/FAILURE. THOSE STILL NEED TO BE BRAINSTORMED*/

// using UnityEngine;
// using System.Text;

// public class ScoringPromptController : MonoBehaviour
// {
//     //[SerializeField] private FloodData scoringData; 
//     private string residentsDescription = "As a representative of the local community, your responsibility is to assess the response to the recent flood. Your priority is the well-being of residents, including safety, housing stability, and overall recovery efforts. Consider whether people received timely assistance, whether displacement was minimized, and if essential needs were met. In less than 100 words, rate the response from 0 to 4 stars based on how effectively these concerns were addressed. Stay under 425 characters.";
//     private string corporateDescription = "You represent the business community affected by the flood. Your assessment focuses on the stability of businesses, protection of assets, and the effectiveness of recovery measures. Consider whether infrastructure was preserved, economic losses were minimized, and whether businesses received sufficient support to rebuild. In less than 100 words, rate the response from 0 to 4 stars based on how well these objectives were achieved. Stay under 425 characters.";
//     private string politicalDescription = "You are the political opinion evaluating the leadership and decision-making during the flood crisis. Your assessment considers the effectiveness of policies, coordination of emergency efforts, and public trust in leadership. Weigh the success of resource allocation, crisis communication, and overall governance. In less than 100 words, rate the response from 0 to 4 stars based on how well these responsibilities were fulfilled. Stay under 425 characters.";
   
//     // ScoringData is a ScriptableObject containing any thresholds, descriptive text, or instructions 
//     // relevant to how the LLM should evaluate each faction.

//     /// <summary>
//     /// Builds a prompt string that the LLMController can send to the model.
//     /// </summary>
//     public string BuildResidentPrompt()
//     {
//         // In a real implementation, you’d likely combine instructions, 
//         // descriptions of thresholds, current state data, and any other relevant context.

//         StringBuilder promptBuilder = new StringBuilder();

//      // Then provide faction-specific context from scoringData (e.g. thresholds, key metrics):
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine(residentsDescription);

//         // Now include real-time values (the current state):
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine("Here is the aftermath of the flood:");
//         promptBuilder.AppendLine($"- Homes flooded: {scoringData.homesFloodedPercent}%");
//         promptBuilder.AppendLine($"- Casualties: {scoringData.casualties}");
//         //promptBuilder.AppendLine($"- Utility downtime: {scoringData.utilityDowntimeHours} hours");
//         // etc. for any relevant data points…

//         // Finally, prompt the LLM to produce a star rating breakdown:
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine("Please respond with the star rating in a clear format. Make sure to start the beginning in this way:");
//         promptBuilder.AppendLine("Example: 'Residents: 4, [rest of response]");

//         return promptBuilder.ToString();
//     }
    
//     public string BuildCorporatePrompt()
//     {
//         // In a real implementation, you’d likely combine instructions, 
//         // descriptions of thresholds, current state data, and any other relevant context.

//         StringBuilder promptBuilder = new StringBuilder();

//      // Then provide faction-specific context from scoringData (e.g. thresholds, key metrics):
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine(corporateDescription);

//         // Now include real-time values (the current state):
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine("Here is the aftermath of the flood:");
//         promptBuilder.AppendLine($"- Businesses affected: {scoringData.businessesAffectedPercent}%");
//         promptBuilder.AppendLine($"- Economic losses: {scoringData.economicLosses}");
//         //promptBuilder.AppendLine($"- Infrastructure damage: {scoringData.infrastructureDamagePercent}%");
//         //etc. for any relevant data points…

//         // Finally, prompt the LLM to produce a star rating breakdown:
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine("Please respond with the star rating in a clear format. Make sure to start the beginning in this way:");
//         promptBuilder.AppendLine("Example: 'Corporate: 4, [rest of response]");

//         return promptBuilder.ToString();
//     }
    
//     public string BuildPoliticalPrompt()
//     {
//         // In a real implementation, you’d likely combine instructions, 
//         // descriptions of thresholds, current state data, and any other relevant context.

//         StringBuilder promptBuilder = new StringBuilder();

//      // Then provide faction-specific context from scoringData (e.g. thresholds, key metrics):
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine(politicalDescription);

//         // Now include real-time values (the current state):
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine("Here is the aftermath of the flood:");
//         promptBuilder.AppendLine($"- Homes flooded: {scoringData.homesFloodedPercent}%");
//         promptBuilder.AppendLine($"- Casualties: {scoringData.casualties}");
//         //promptBuilder.AppendLine($"- Utility downtime: {scoringData.utilityDowntimeHours} hours");
//         promptBuilder.AppendLine($"- Businesses affected: {scoringData.businessesAffectedPercent}%");
//         promptBuilder.AppendLine($"- Economic losses: {scoringData.economicLosses}");
//         //promptBuilder.AppendLine($"- Infrastructure damage: {scoringData.infrastructureDamagePercent}%");
        
//         // etc. for any relevant data points…

//         // Finally, prompt the LLM to produce a star rating breakdown:
//         promptBuilder.AppendLine();
//         promptBuilder.AppendLine("Please respond with the star rating in a clear format. Make sure to start the beginning in this way:");
//         promptBuilder.AppendLine("Example: 'Political: 4, [rest of response]");

//         return promptBuilder.ToString();
//     }
// }
