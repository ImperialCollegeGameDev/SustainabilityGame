// assigned to each energy utility object

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

    public Utility(int id, string name, PowerType type, int cost, int output, int emission)
    {
        this.id = id;
        this.name = name;
        this.type = type;
        this.cost = cost;
        this.output = output;
        this.emission = emission;
            
    }
}
