using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;

namespace VoiceChanger
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // This is the standard entry point for a Synthesis Patcher.
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "vot_patcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // --- Configuration ---
            // A dictionary that maps a source voice to a list of potential target voices.
            // Key: The EditorID of the voice type you want to find and replace.
            // Value: A list of EditorIDs for the voices you want to randomly assign for that key.
            var voiceMappings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "MaleCommoner", new List<string> { "VOT_MaleCommoner01", "VOT_MaleCommoner02" } },
                { "FemaleCommoner", new List<string> { "VOT_FemaleCommoner01", "VOT_FemaleCommoner02" } }
                // Add more mappings here as needed, e.g.:
            };
            // --- End of Configuration ---

            var random = new Random();
            int patchedNpcCount = 0;
            int keptNpcCount = 0;

            // Step 1: Resolve all target voice strings into actual voice records upfront.
            // This is much more efficient than looking them up repeatedly inside the loop.
            var resolvedVoiceMappings = new Dictionary<string, List<IVoiceTypeGetter>>(StringComparer.OrdinalIgnoreCase);
            
            Console.WriteLine("Resolving voice mappings...");
            foreach (var mapping in voiceMappings)
            {
                var sourceVoice = mapping.Key;
                var targetVoices = mapping.Value;
                var resolvedTargets = new List<IVoiceTypeGetter>();

                foreach (var targetEditorId in targetVoices)
                {
                    if (state.LinkCache.TryResolve<IVoiceTypeGetter>(targetEditorId, out var voice))
                    {
                        resolvedTargets.Add(voice);
                    }
                    else
                    {
                        Console.WriteLine($" -> WARNING: For source '{sourceVoice}', could not find target '{targetEditorId}'. It will be skipped.");
                    }
                }

                if (resolvedTargets.Any())
                {
                    resolvedVoiceMappings[sourceVoice] = resolvedTargets;
                    Console.WriteLine($" -> Mapping for '{sourceVoice}' successfully resolved with {resolvedTargets.Count} target(s).");
                }
                else
                {
                    Console.WriteLine($" -> WARNING: No valid target voices found for source '{sourceVoice}'. It will be skipped entirely.");
                }
            }
            
            Console.WriteLine("\nProcessing NPCs...");

            // Step 2: Iterate through all NPCs and apply the mappings.
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                if (npc.Voice.IsNull) continue;

                if (state.LinkCache.TryResolve<IVoiceTypeGetter>(npc.Voice.FormKey, out var currentVoice))
                {
                    // Check if the NPC's current voice is one of our source voices (a key in our dictionary).
                    if (currentVoice.EditorID != null && resolvedVoiceMappings.TryGetValue(currentVoice.EditorID, out var availableTargets))
                    {
                        // We found a match! Now use the specific list of targets for this source voice.
                        int choice = random.Next(0, availableTargets.Count + 1);

                        if (choice == 0)
                        {
                            keptNpcCount++;
                            continue; // Leave the voice as is.
                        }
                        else
                        {
                            var targetVoice = availableTargets[choice - 1];
                            var npcOverride = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                            npcOverride.Voice.SetTo(targetVoice);
                            patchedNpcCount++;
                        }
                    }
                }
            }
            
            // Step 3: Print the final summary.
            Console.WriteLine("\n--- Voice Patcher Summary ---");
            Console.WriteLine($"Total NPCs patched: {patchedNpcCount}");
            Console.WriteLine($"Total NPCs kept with original voice: {keptNpcCount}");
            Console.WriteLine("-----------------------------");
        }
    }
}
