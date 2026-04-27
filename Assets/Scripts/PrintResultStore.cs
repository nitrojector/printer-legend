using UnityEngine;

public static class PrintResultStore
{
    public static Texture2D FinalPrint { get; private set; }
    public static Sprite ReferenceSprite { get; private set; }
    public static int CompletedLevelIndex { get; private set; }

    public static void Store(Texture2D source, Sprite reference, int levelIndex)
    {
        if (FinalPrint != null)
            Object.Destroy(FinalPrint);

        var copy = new Texture2D(source.width, source.height, source.format, false);
        copy.SetPixels32(source.GetPixels32());
        copy.Apply();

        FinalPrint = copy;
        ReferenceSprite = reference;
        CompletedLevelIndex = levelIndex;
    }
}
