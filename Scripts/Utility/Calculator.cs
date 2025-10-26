using Godot;

public static class Calculator
{
    public const bool ON = true;
    public const bool OFF = false;
    
    private static float
        playerSpeed,
        jumpHeight,
        jumpDist,
        jumpDistAtApex,
        jumpForce,
        timeTilApex,
        jumpGravity;
    
    private static float GetHeight(float g, float t) => -.5f * g * Mathf.Pow(t, 2);
    private static float GetGravity(float h, float t) => (2f * h) / Mathf.Pow(t, 2);
    private static float GetInitialVelocity(float h, float t) => (2f * h) / t;
    private static float GetTimeTilApex(float Xh_dist, float Vx_rate) => Xh_dist / Vx_rate;
    
    // public static float JumpTimeTilApex => timeTilApex;
    // public static float JumpForce => jumpForce;
    // public static float JumpGravity => jumpGravity;
    
    public static (float timeTilApex, float jumpForce, float jumpGravity, float jumpDistAtApex) JumpCalculator(float newHeight, float newDist, float moveSpeed)
    {
        playerSpeed = moveSpeed;

        jumpHeight = newHeight;

        jumpDist = newDist;

        jumpDistAtApex = jumpDist / 2;

        timeTilApex = jumpDistAtApex / playerSpeed; //GetTimeTilApex(jumpDistAtApex, playerSpeed);
        jumpForce = 2 * jumpHeight / timeTilApex; // GetInitialVelocity(jumpHeight, timeTilApex);
        jumpGravity = jumpForce / timeTilApex; //GetGravity(jumpHeight, timeTilApex);

        // DebugJumpValues();

        return (timeTilApex, jumpForce, jumpGravity, jumpDistAtApex);
    }
    
    public static float PercentToDecimal(float value) => (value / 100f);
    public static float PercentToMultiplier(float value) => 1 + PercentToDecimal (value);

    public static float DecimalToPercent(float value) => (value * 100f);
    public static float MultiplierToPercent(float value) => DecimalToPercent(value) - 1;

    public static bool RollChance(float chance)
    {
        switch (chance)
        {
            case <= Mathf.Epsilon:
                return false;

            // Debug.Log("Chance is: " + chance);
            case > .99f:
                // Debug.Log("Success!");
                return true;
        }

        var roll = GD.Randf();

        return roll < chance;
    }

    public static bool RollPercentChance(float chance)
    {
        switch (chance)
        {
            case <= Mathf.Epsilon:
                return false;

            // Debug.Log("Chance is: " + chance);
            case > 99f:
                // Debug.Log("Success!");
                return true;
        }

        var roll = (GD.Randf() * 100).RoundToPrecision();

        return roll < chance;
    }
    
    public static int BinaryPow(int power)
    {
        return (int)Mathf.Pow(2, power);
    }
    
    public static float RoundToPrecision(float value, int places)
    {
        var tens = Mathf.Pow(10, Mathf.Max(0, places));
        float rounded = Mathf.Round((value * tens));
        return rounded / tens;
    }

    public static Color AverageColor(Color color1, Color color2)
    {
        var colorR = Avg(color1.R, color2.R);
        var colorG = Avg(color1.G, color2.G);
        var colorB = Avg(color1.B, color2.B);
        return new Color(colorR, colorG, colorB);
    }

    public static float Avg(float a, float b)
    {
        return (a + b) / 2;
    }

    public static float Avg(float a, float b, float c)
    {
        return (a + b + c) / 3;
    }

    public static float Avg(float a, float b, float c, float d)
    {
        return (a + b + c + d) / 4;
    }
}