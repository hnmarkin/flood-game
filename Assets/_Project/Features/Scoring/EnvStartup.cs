using UnityEngine;
using System.IO;

public static class EnvStartup
{
    // This attribute ensures it runs before Unity loads the first scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        // Points to root folder
        string envPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../API_Key.env"));
        //Debug.Log("Loading environment variables from: " + envPath);
        EnvLoader.LoadEnvFile(envPath);
        
        // Example of retrieving a variable you just set:
        string apiKey = System.Environment.GetEnvironmentVariable("MY_API_KEY");
        //Debug.Log("API Key During EnvStartup: " + apiKey);
    }
}

public static class EnvLoader
{
    public static void LoadEnvFile(string envFilePath)
    {
        if (!File.Exists(envFilePath)) return;
        
        foreach (var line in File.ReadAllLines(envFilePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) 
                continue; // Skip comments and empty lines.
            
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                System.Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
