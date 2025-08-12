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
                .SetTypicalOpen(GameRelease.SkyrimSE, "VOTpatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // --- Configuration ---
            // The EditorID of the voice type you want to find.
            var sourceVoiceEditorId = "MaleCommoner";
            // The EditorID of the voice type you want to assign.
            // This must exist in your load order (from the base game or a mod).
            var targetVoiceEditorId = "VOT_MaleCommoner01";
            // --- End of Configuration ---

            // First, we need to find the voice type record we want to assign.
            // We use the LinkCache to look it up by its EditorID. This is safer than
            // using a hard-coded FormID, which can change between mod versions.
            if (!state.LinkCache.TryResolve<IVoiceTypeGetter>(targetVoiceEditorId, out var targetVoice))
            {
                // If the voice type doesn't exist in the user's load order, we can't proceed.
                // We print an error and stop the patcher.
                Console.WriteLine($"FATAL: Could not find the target voice type with EditorID '{targetVoiceEditorId}'.");
                Console.WriteLine($"Please ensure the mod that provides this voice is enabled in your load order.");
                return;
            }

            Console.WriteLine($"Successfully found target voice: {targetVoice.EditorID} [{targetVoice.FormKey}]");

            // Now, we iterate through all Non-Player Character (NPC) records in the load order.
            // .Npc() gets all the NPC records.
            // .WinningOverrides() ensures we only look at the final version of the NPC
            // after all mods have made their changes.
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                // Some NPCs might not have a voice type assigned. We skip them.
                if (npc.Voice.IsNull) continue;

                // We resolve the link to the NPC's current voice type record.
                if (state.LinkCache.TryResolve<IVoiceTypeGetter>(npc.Voice.FormKey, out var currentVoice))
                {
                    // We check if the EditorID of the NPC's current voice matches our source voice.
                    // We use StringComparison.OrdinalIgnoreCase to be safe.
                    if (currentVoice.EditorID?.Equals(sourceVoiceEditorId, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // We found an NPC that needs to be patched!
                        Console.WriteLine($"Found NPC '{npc.EditorID}' [{npc.FormKey}] with voice '{sourceVoiceEditorId}'.");

                        // We create a copy of the NPC record in our new patch file.
                        // GetOrAddAsOverride will either get an existing override from the patch
                        // or create a new one based on the winning override from the load order.
                        var npcOverride = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                        // Finally, we change the voice of our overridden NPC to the target voice.
                        npcOverride.Voice.SetTo(targetVoice);

                        Console.WriteLine($" -> Patched to use voice '{targetVoiceEditorId}'.");
                    }
                }
            }
        }
    }
}
