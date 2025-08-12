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
                .SetTypicalOpen(GameRelease.SkyrimSE, "VOT_patcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // --- Configuration ---
            // The EditorID of the voice type you want to find.
            var sourceVoiceEditorId = "MaleCommoner";
            // A list of EditorIDs for the voice types you want to randomly assign.
            // These must exist in your load order (from the base game or a mod).
            var targetVoiceEditorIds = new List<string>
            {
                "VOT_MaleCommoner01",
                "VOT_MaleCommoner02"
            };
            // --- End of Configuration ---

            // Create a single Random object to use for all decisions.
            var random = new Random();

            // Find all the target voice type records from our list of EditorIDs.
            var resolvedTargetVoices = new List<IVoiceTypeGetter>();
            foreach (var editorId in targetVoiceEditorIds)
            {
                if (state.LinkCache.TryResolve<IVoiceTypeGetter>(editorId, out var voice))
                {
                    resolvedTargetVoices.Add(voice);
                    Console.WriteLine($"Successfully found target voice: {voice.EditorID} [{voice.FormKey}]");
                }
                else
                {
                    // If a voice can't be found, we print a warning but continue.
                    Console.WriteLine($"WARNING: Could not find target voice type with EditorID '{editorId}'. It will be skipped.");
                }
            }

            // If we couldn't find ANY of the target voices, we can't do anything.
            if (resolvedTargetVoices.Count == 0)
            {
                Console.WriteLine($"FATAL: None of the specified target voices could be found. Patcher cannot continue.");
                return;
            }

            // Now, we iterate through all Non-Player Character (NPC) records in the load order.
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                // Skip NPCs with no voice assigned.
                if (npc.Voice.IsNull) continue;

                if (state.LinkCache.TryResolve<IVoiceTypeGetter>(npc.Voice.FormKey, out var currentVoice))
                {
                    // Check if the NPC's current voice matches our source voice.
                    if (currentVoice.EditorID?.Equals(sourceVoiceEditorId, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        Console.WriteLine($"Found NPC '{npc.EditorID}' [{npc.FormKey}] with voice '{sourceVoiceEditorId}'.");

                        // We have N target voices, plus 1 option to "do nothing".
                        // So we generate a random number from 0 to N.
                        int choice = random.Next(0, resolvedTargetVoices.Count + 1);

                        if (choice == 0)
                        {
                            // A choice of 0 means we leave the voice as it is.
                            Console.WriteLine($" -> Keeping original voice.");
                            continue; // Move to the next NPC
                        }
                        else
                        {
                            // Any other choice corresponds to an index in our list of resolved voices.
                            // Since our choice is 1-based and list indices are 0-based, we subtract 1.
                            var targetVoice = resolvedTargetVoices[choice - 1];

                            // Create an override for the NPC in our patch.
                            var npcOverride = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                            // Change the voice to our randomly selected target.
                            npcOverride.Voice.SetTo(targetVoice);

                            Console.WriteLine($" -> Patched to use voice '{targetVoice.EditorID}'.");
                        }
                    }
                }
            }
        }
    }
}
