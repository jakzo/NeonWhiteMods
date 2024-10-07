using System;
using System.Threading.Tasks;
using Jakzo.NeonWhiteMods;

public static class ListCheaters
{
    public static void Run()
    {
        Task.Run(async () =>
        {
            try
            {
                await Mod.Instance.ListCheaters();
            }
            catch (Exception ex)
            {
                Mod.Instance.LoggerInstance.Error($"Failed to list cheaters:\n{ex}");
            }
        });
    }
}
