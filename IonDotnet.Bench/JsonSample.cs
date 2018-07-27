using System.Collections.Generic;

namespace IonDotnet.Bench
{
    public class Friend
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class RootObject
    {
        public string _id { get; set; }
        public int index { get; set; }
        public string guid { get; set; }
        public bool isActive { get; set; }
        public string balance { get; set; }
        public string picture { get; set; }
        public int age { get; set; }
        public string eyeColor { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public string company { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string address { get; set; }
        public string about { get; set; }
        public string registered { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public List<string> tags { get; set; }
        public List<Friend> friends { get; set; }
        public string greeting { get; set; }
        public string favoriteFruit { get; set; }
    }
}
