namespace FiveStack.Utilities
{
    public static class DamageUtility
    {
        public static string HitGroupToString(int hitGroup)
        {
            switch (hitGroup)
            {
                case 0:
                    return "Body";
                case 1:
                    return "Head";
                case 2:
                    return "Chest";
                case 3:
                    return "Stomach";
                case 4:
                    return "Left Arm";
                case 5:
                    return "Right Arm";
                case 6:
                    return "Left Leg";
                case 7:
                    return "Right Leg";
                case 10:
                    return "Gear";
                default:
                    return "Unknown";
            }
        }
    }
}
