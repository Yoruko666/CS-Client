using System.Collections.Generic;

public class Maps
{
    public List<string> maps = new List<string>();
    private static Maps instance;

    public static Maps Instance
    {
        get
        {
            instance ??= new Maps();
            return instance;
        }
    }

    Maps()
    {
        maps.Add("Sand Sea Lost City");
    }
}