using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToModelLib
{
    public class SettingModel
    {
        public SettingModel()
        {
        }

        public SettingModel(string type, string name)
        {
            Type = type;
            Name = name;
        }

        public string Type { get; set; }

        public string Name { get; set; }
    }
}
