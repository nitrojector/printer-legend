using UnityEngine;
using UnityEngine.InputSystem;

public class PrinterTypewriter_Game2 : MonoBehaviour
{
    public Texture2D referenceTex;

    // hard coded paper size (square)
    [Header("Paper Texture")]
    public int paperW = 256;
    public int paperH = 256;

    [Header("Typewriter Window")]
    [Range(0.05f, 0.8f)] public float visibleWindowHeight01 = 0.25f;
    [Range(0f, 2f)] public float startBlankLeadInWindows = 1.0f;

    [Header("Printing Motion")]
    public float scanSpeed = 0.6f;  // printer head speed
    public float lineStep = 0.03f;  // distance increase per carriage return
    public float moveTime = 0.1f; // time between each head step movement
    public float moveStep = 0.03f; // how much the head moves each step

    [Header("Ink")]
    public int brushRadius = 2;
    public bool requireHoldSpaceToPrint = true;

    [Header("Reveal")]
    public float revealFadeSeconds = 0.6f;   // fade-in duration at the end

    // when space is held to return the bar sometimes it prints ink on the left side of the paper immediately so the safety prevents this
    [Header("Print Safety")]
    [Range(0f, 0.2f)] public float leftNoPrint01 = 0.03f;
    [Range(0f, 0.2f)] public float rightNoPrint01 = 0.00f;

    [Header("Audio SFX")]
    public AudioSource moveLoopSource;
    public AudioSource printLoopSource;
    public AudioSource oneShotSource; 

    public AudioClip moveLoopClip;
    public AudioClip printLoopClip; 
    public AudioClip carriageReturnClip;
    public AudioClip revealClip;

    private InputAction spaceAction;

    // progress through the real page
    private float progress01 = 0f;
    private float headX = 0f;

    // texture variables
    private Texture2D paperTex;
    private Texture2D red1x1;
    private Texture2D dark1x1;
    private Color paperBase;
    private Color paperCol;
    private Color lineCol;

    private Color[] paperPixels;
    private bool dirtyThisFrame = false;

    private const float DESIGN_W = 1280f;
    private const float DESIGN_H = 720f;

    private Rect machineRect;
    private Rect paperWindowRect;
    private Rect refRect;

    private const float PRINT_LINE_MARGIN_FROM_BOTTOM_PX = 12f;

    private enum State
    {
        Scanning,      // head moves L->R, can print while holding space
        AwaitReset,    // head at right edge, waiting for a space tap to reset + advance line
        Finished       // reveal full paper
    }

    private State state = State.Scanning;

    // hold if space is being held
    private bool prevSpaceHeld = false;

    // reveal fade
    private float finishedTime = -1f;

    private float moveTimer = 0.0f;

    void OnEnable()
    {
        spaceAction = new InputAction("Space", InputActionType.Button, "<Keyboard>/space");
        spaceAction.Enable();
    }

    void OnDisable()
    {
        if (spaceAction != null) spaceAction.Disable();
    }

    void Start()
    {
        // create paper texture + grey guide lines so movement is visible
        paperTex = new Texture2D(paperW, paperH, TextureFormat.RGBA32, false);

        //paperBase = new Color(0.97f, 0.97f, 0.95f, 0f);
        paperCol = new Color(0.97f, 0.97f, 0.95f, 1f);
        
        lineCol   = new Color(0.90f, 0.90f, 0.88f, 1f);

        paperPixels = new Color[paperW * paperH];
        for (int y = 0; y < paperH; y++)
        {
            bool isLine = (y % 16 == 0);
            Color c = isLine ? lineCol : paperCol;
            for (int x = 0; x < paperW; x++)
                paperPixels[y * paperW + x] = c;
        }

        paperTex.SetPixels(paperPixels);
        paperTex.Apply(false);

        red1x1 = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        red1x1.SetPixel(0, 0, Color.red);
        red1x1.Apply(false);

        dark1x1 = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        dark1x1.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 1f));
        dark1x1.Apply(false);

        // layout of the UI
        //machineRect     = new Rect(40, 450, 640, 250);
        paperWindowRect = new Rect(180, 210, 320, 280);
        refRect         = new Rect(810, 280, 320, 280);
        
        

        ResetRun();
    }

    void ResetRun()
    {
        // clear paper back to lined base
        for (int y = 0; y < paperH; y++)
        {
            bool isLine = (y % 16 == 0);
            Color c = isLine ? lineCol : paperCol;
            for (int x = 0; x < paperW; x++)
                paperPixels[y * paperW + x] = c;
        }
        paperTex.SetPixels(paperPixels);
        paperTex.Apply(false);
        dirtyThisFrame = false;

        progress01 = 0f;
        headX = 0f;
        state = State.Scanning;

        finishedTime = -1f;
    }

    void Update()
    {
        bool spaceHeld = (spaceAction != null) && spaceAction.IsPressed();
        bool spaceJustPressed = spaceHeld && !prevSpaceHeld;
        prevSpaceHeld = spaceHeld;

        // SFX
        if (state == State.Scanning)
        {
            if (moveLoopSource != null && moveLoopClip != null && !moveLoopSource.isPlaying)
            {
                moveLoopSource.clip = moveLoopClip;
                moveLoopSource.loop = true;
                moveLoopSource.Play();
            }
        }
        else
        {
            if (moveLoopSource != null && moveLoopSource.isPlaying)
                moveLoopSource.Stop();
        }

        if (state == State.Finished)
        {
            if (spaceJustPressed)
                ResetRun();
            return;
        }

        if (state == State.AwaitReset)
        {
            // must press space to advance the line
            if (spaceJustPressed)
            {
                headX = 0f;

                // --- CARRIAGE RETURN ONE-SHOT ---
                if (oneShotSource != null && carriageReturnClip != null)
                    oneShotSource.PlayOneShot(carriageReturnClip);

                // advance the paper one line
                progress01 += lineStep;

                if (progress01 >= 1f)
                {
                    progress01 = 1f;
                    EnterFinished();
                }
                else
                {
                    state = State.Scanning;
                }
            }

            // Apply any ink changes if any just in case
            ApplyPaperIfDirty();
            return;
        }

        // --- state == Scanning ---
        // Move head left -> right
        
        // headX += scanSpeed * Time.deltaTime;

        // NOTE: changed to fixed step movement to make drawing more "deterministic"?
        moveTimer += Time.deltaTime;
        if (moveTimer >= moveTime)
        {
            moveTimer = 0.0f;
            headX += 0.02f;
        }

        bool shouldPrint = requireHoldSpaceToPrint ? spaceHeld : true;

        if (state == State.Scanning && shouldPrint)
        {
            StampInkAtPrintLine();

            // --- PRINT LOOP (inking) ---
            if (printLoopSource != null && printLoopClip != null && !printLoopSource.isPlaying)
            {
                printLoopSource.clip = printLoopClip;
                printLoopSource.loop = true;
                printLoopSource.Play();
            }
        }
        else
        {
            if (printLoopSource != null && printLoopSource.isPlaying)
                printLoopSource.Stop();
        }


        // if at the right edge stop and wait for player to reset
        if (headX >= 1f)
        {
            headX = 1f;
            state = State.AwaitReset;

            // --- IMPORTANT: stop printing audio immediately when carriage stops ---
            StopPrintLoop();
        }


        ApplyPaperIfDirty();
    }

    void EnterFinished()
    {
        state = State.Finished;
        finishedTime = Time.time;

        // Stop loops
        if (moveLoopSource != null && moveLoopSource.isPlaying) moveLoopSource.Stop();
        if (printLoopSource != null && printLoopSource.isPlaying) printLoopSource.Stop();

        // Reveal sting
        if (oneShotSource != null && revealClip != null)
            oneShotSource.PlayOneShot(revealClip);
    }


    void ApplyPaperIfDirty()
    {
        if (!dirtyThisFrame) return;
        paperTex.SetPixels(paperPixels);
        paperTex.Apply(false);
        dirtyThisFrame = false;
    }

    // stamp ink on the row currently under the print line
    void StampInkAtPrintLine()
    {
        // prevent accidental left edge stamps when starting a new line
        if (headX < leftNoPrint01) return;
        if (headX > 1f - rightNoPrint01) return;

        float winH = Mathf.Clamp01(visibleWindowHeight01);

        // virtual feed includes extra blank above the real page
        float leadUV = startBlankLeadInWindows * winH;
        float virtualHeight = 1f + leadUV;

        float startBandBottom = virtualHeight - winH;
        float bandBottomVirtual = Mathf.Lerp(startBandBottom, 0f, progress01);
        float bandTopVirtual = bandBottomVirtual + winH;

        // print line is near bottom of slot
        float yFromTop01 = (paperWindowRect.height - PRINT_LINE_MARGIN_FROM_BOTTOM_PX) / paperWindowRect.height;
        yFromTop01 = Mathf.Clamp01(yFromTop01);

        float printVirtualY = bandTopVirtual - (yFromTop01 * winH);

        // if still in blank lead region, do nothing
        if (printVirtualY > 1f) return;

        printVirtualY = Mathf.Clamp01(printVirtualY);

        int px = Mathf.Clamp((int)(headX * (paperW - 1)), 0, paperW - 1);
        int py = Mathf.Clamp((int)(printVirtualY * (paperH - 1)), 0, paperH - 1);

        for (int oy = -brushRadius; oy <= brushRadius; oy++)
        {
            for (int ox = -brushRadius; ox <= brushRadius; ox++)
            {
                if (ox * ox + oy * oy > brushRadius * brushRadius) continue;

                int x = px + ox;
                int y = py + oy;
                if (x < 0 || x >= paperW || y < 0 || y >= paperH) continue;

                paperPixels[y * paperW + x] = Color.black;
            }
        }

        dirtyThisFrame = true;
    }

    void OnGUI()
    {
        // scale UI
        Matrix4x4 old = GUI.matrix;
        float sx = Screen.width / DESIGN_W;
        float sy = Screen.height / DESIGN_H;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));

        // finished reveal overlay
        if (state == State.Finished)
        {
            DrawRevealOverlay();
            GUI.matrix = old;
            return;
        }

        bool spaceHeld = (spaceAction != null) && spaceAction.IsPressed();

        // HUD
        GUI.Label(new Rect(40, 40, 1200, 24), "SPACE = hold to print, tap to carriage-return at end of line.");
        GUI.Label(new Rect(40, 64, 1200, 20),
            "spaceHeld: " + spaceHeld +
            " | state: " + state +
            " | progress01: " + progress01.ToString("0.00") +
            " | headX: " + headX.ToString("0.00"));

        // Machine
        GUI.DrawTexture(machineRect, dark1x1, ScaleMode.StretchToFill, false);
        GUI.Label(new Rect(machineRect.x + 50, machineRect.y + 150, 400, 20), "TYPEWRITER");

        // Paper slot
        DrawPaperWindow(paperWindowRect);

        // Print line at bottom + head marker
        float headScreenX = paperWindowRect.x + headX * paperWindowRect.width;
        float printLineY  = paperWindowRect.y + paperWindowRect.height - PRINT_LINE_MARGIN_FROM_BOTTOM_PX;

        GUI.DrawTexture(new Rect(paperWindowRect.x, printLineY, paperWindowRect.width, 2f), red1x1);
        GUI.DrawTexture(new Rect(headScreenX - 5, printLineY - 5, 10, 10), red1x1);

        // Reference + scanline (scanline is based on progress)
        if (referenceTex != null)
        {
            GUI.DrawTexture(refRect, referenceTex, ScaleMode.ScaleToFit, true);
            float scanY = refRect.y + progress01 * refRect.height;
            GUI.DrawTexture(new Rect(refRect.x, scanY, refRect.width, 3f), red1x1);
        }

        // Visual hint when waiting for manual reset
        if (state == State.AwaitReset)
        {
            GUI.Label(new Rect(paperWindowRect.x, paperWindowRect.y + paperWindowRect.height + 10, 600, 20),
                "End of line — tap SPACE to carriage return.");
        }

        GUI.matrix = old;
    }

    void DrawRevealOverlay()
    {
        float t = 1f;
        if (finishedTime > 0f && revealFadeSeconds > 0.001f)
            t = Mathf.Clamp01((Time.time - finishedTime) / revealFadeSeconds);

        // dim background
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f * t);
        GUI.DrawTexture(new Rect(0, 0, DESIGN_W, DESIGN_H), dark1x1);
        GUI.color = old;

        GUI.color = new Color(1f, 1f, 1f, t);
        GUI.Label(new Rect(40, 40, 1200, 30), "FINAL PRINT (tap SPACE to restart)");

        // create two side by side frames one for the user drawing one for the ref image
        Rect leftFrame  = new Rect(120, 110, 500, 560);
        Rect rightFrame = new Rect(660, 110, 500, 560);

        // frames
        GUI.DrawTexture(leftFrame, dark1x1, ScaleMode.StretchToFill, false);
        GUI.DrawTexture(rightFrame, dark1x1, ScaleMode.StretchToFill, false);

        // labels
        GUI.Label(new Rect(leftFrame.x, leftFrame.y - 26, 300, 20), "Your Print");
        GUI.Label(new Rect(rightFrame.x, rightFrame.y - 26, 300, 20), "Reference");

        // inner rects
        Rect leftInner  = new Rect(leftFrame.x + 16, leftFrame.y + 16, leftFrame.width - 32, leftFrame.height - 32);
        Rect rightInner = new Rect(rightFrame.x + 16, rightFrame.y + 16, rightFrame.width - 32, rightFrame.height - 32);

        // Draw textures
        GUI.DrawTexture(leftInner, paperTex, ScaleMode.ScaleToFit, false);

        if (referenceTex != null)
            GUI.DrawTexture(rightInner, referenceTex, ScaleMode.ScaleToFit, true);
        else
            GUI.Label(new Rect(rightInner.x, rightInner.y, 400, 20), "No referenceTex assigned");

        GUI.color = old;
    }


    void DrawPaperWindow(Rect windowRect)
    {
        float winH = Mathf.Clamp01(visibleWindowHeight01);

        float leadUV = startBlankLeadInWindows * winH;
        float virtualHeight = 1f + leadUV;

        float startBandBottom = virtualHeight - winH;
        float bandBottomVirtual = Mathf.Lerp(startBandBottom, 0f, progress01);
        float bandTopVirtual = bandBottomVirtual + winH;

        GUI.Box(new Rect(windowRect.x - 2, windowRect.y - 2, windowRect.width + 4, windowRect.height + 4), "");

        GUI.BeginGroup(windowRect);

        // blank portion above the real page
        float blankAbove = Mathf.Max(0f, bandTopVirtual - 1f);
        blankAbove = Mathf.Clamp(blankAbove, 0f, winH);
        float blankPixelH = (blankAbove / winH) * windowRect.height;

        if (blankPixelH > 0.5f)
        {
            Color old = GUI.color;
            GUI.color = paperBase;
            GUI.DrawTexture(new Rect(0, 0, windowRect.width, blankPixelH), dark1x1);
            GUI.color = old;
        }

        float remainingPixelH = windowRect.height - blankPixelH;
        if (remainingPixelH > 0.5f)
        {
            float realTopVirtual = Mathf.Min(bandTopVirtual, 1f);
            float realBottomVirtual = Mathf.Min(bandBottomVirtual, 1f);

            float uvY = realBottomVirtual;
            float uvH = Mathf.Max(0.0001f, realTopVirtual - realBottomVirtual);

            GUI.DrawTextureWithTexCoords(
                new Rect(0, blankPixelH, windowRect.width, remainingPixelH),
                paperTex,
                new Rect(0f, uvY, 1f, uvH)
            );
        }

        GUI.EndGroup();
    }

    void StopPrintLoop()
    {
        if (printLoopSource != null && printLoopSource.isPlaying)
            printLoopSource.Stop();
    }
}