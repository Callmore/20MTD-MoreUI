using System.Linq;
using TMPro;
using UnityEngine;

namespace Callmore.MoreUI;

public static class Helpers
{
    public static TMP_FontAsset GetTMPFontAssetByName(string fontName)
    {
        return Resources
            .FindObjectsOfTypeAll<TMP_FontAsset>()
            .First((font) => font.name == fontName);
    }
}
