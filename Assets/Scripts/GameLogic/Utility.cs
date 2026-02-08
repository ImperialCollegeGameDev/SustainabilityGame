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

public class Utility
{
    public int id;
    public string name;
    public PowerType type;
    
    int cost;
    int output;
    int emission;

    public int Cost => cost;
    public int Output => output;
    public int Emission => emission;

    public Utility(int id, string name, PowerType type, int cost, int output, int emission)
    {
        this.id = id;
        this.name = name;
        this.type = type;
        this.cost = cost;
        this.output = output;
        this.emission = emission;
    }

    public override string ToString()
    {
        return $"Utility(id={id}, name={name}, type={type}, cost={cost}, output={output}, emission={emission})";
    }
}
