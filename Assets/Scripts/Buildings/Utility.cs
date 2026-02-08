/// <summary>
/// Pure game-logic model for a single utility unit (power plant).
/// </summary>

public enum PowerType
{
    Conventional,
    Solar,
    Wind,
    Nuclear,
}

[System.Serializable]
public class Utility
{
    public PowerType type;
    public int Output;
    public int Emission;

    public Utility(PowerType type, int output, int emission)
    {
        this.type = type;
        this.Output = output;
        this.Emission = emission;
    }

    public override string ToString()
    {
        return $"Utility(type={type}, output={Output}, emission={Emission})";
    }
}
