using Unity.Entities;

//A class to hold all data types to do with the universe generation
public static class UniverseData
{
    //Static function to create star data from the scale given, mainly calculates the type of star based on size.
    public static StarData CreateStarData(float scale)
    {
        StarData starData = new StarData
        {
            starSize = scale
        };

        if (scale > 0.96f)
        {
            starData.starType = StarType.NeutronStar;
        }
        else if(scale > 0.83f)
        {
            starData.starType = StarType.RedSuperGiant;
        }
        else if(scale > 0.70f)
        {
            starData.starType = StarType.RedGiant;
        }
        else if(scale > 0.20f)
        {
            starData.starType = StarType.Main;
        }
        else if(scale > 0.10f)
        {
            starData.starType = StarType.WhiteDwarf;
        }
        else 
        {
            starData.starType = StarType.BlackDwarf;
        }
        return starData;
    }
}

//Data struct to be placed on star entities
public struct StarData : IComponentData
{
    public StarType starType;
    public float starSize;
}

//Enum for star types.
public enum StarType
{
    Main,
    RedGiant,
    RedSuperGiant,
    WhiteDwarf,
    BlackDwarf,
    NeutronStar,
}