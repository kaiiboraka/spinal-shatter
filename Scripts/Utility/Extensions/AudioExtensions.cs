using Godot;

public static class AudioExtensions
{
    public static double GetMaxLength(this AudioStreamRandomizer randomizer)
    {
        double maxLength = 0.0;

        if (randomizer == null)
        {
            return maxLength;
        }

        int streamsCount = randomizer.GetStreamsCount();
        if (streamsCount == 0)
        {
            return maxLength;
        }
        for (int i = 0; i < streamsCount; i++)
        {
            double length = randomizer.GetStream(i).GetLength();
            if (length > maxLength)
            {
                maxLength = length;
            }
        }
        return maxLength;
    }
}
