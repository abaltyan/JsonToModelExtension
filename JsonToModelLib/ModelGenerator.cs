using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToModelLib
{
    public class ModelGenerator
    {
        #region fields

        private string jsonPath;
        private string projectName;
        private string outputFolder;
        private string rootClassName;
        private string rootNamespace;
        private string entitiesFolder;
        private string[] itemsToDecrypt;

        #endregion

        public ModelGenerator(string jsonPath, string projectName, string outputFolder, string rootClassName, string rootNamespace, string entitiesFolder, string[] itemsToDecrypt)
        {
            this.jsonPath = jsonPath;
            this.projectName = projectName;
            this.outputFolder = outputFolder;
            this.rootClassName = rootClassName;
            this.rootNamespace = rootNamespace;
            this.entitiesFolder = entitiesFolder;
            this.itemsToDecrypt = itemsToDecrypt;
        }

        private Dictionary<string, List<SettingModel>> _settingsObjects = new Dictionary<string, List<SettingModel>>();

        public (bool isSuccess, string message) Generate()
        {
            try
            {
                _settingsObjects.Add("", new List<SettingModel>());

                JToken jSettings = ReadJsonFile(jsonPath);

                CollectJsonFields(jSettings);

                OutputModels();

                return (true, "Settings are succssesfuly created.");
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.ToString());
            }
        }

        #region private methods
        
        private void CollectJsonFields(JToken jToken)
        {
            switch (jToken.Type)
            {
                case JTokenType.Object:

                    foreach (var child in jToken.Children<JProperty>())
                        CollectJsonFields(child);

                    break;
                case JTokenType.Array:
                    break;
                case JTokenType.Property:

                    var jProp = (JProperty)jToken;

                    if (jProp.Value.HasValues)
                    {
                        if (jProp.Value.Type == JTokenType.Object)
                        {
                            _settingsObjects.Add(jProp.Name, new List<SettingModel>());

                            SetSetting(jProp, ItemType.Class);
                        }
                        else if (jProp.Value.Type == JTokenType.Array)
                        {
                            SetSetting(jProp, ItemType.Array);
                        }
                    }

                    CollectJsonFields(jProp.Value);
                    break;
                default:

                    SetSetting(jToken, ItemType.Item);

                    break;
            }
        }

        private void SetSetting(JToken jToken, ItemType ItemType)
        {
            if (jToken != null)
            {
                (string name, string parentName) = GetParentNames(jToken.Path);

                if (_settingsObjects.ContainsKey(parentName))
                {
                    if (ItemType == ItemType.Class)
                    {
                        _settingsObjects[parentName].Add(new SettingModel(name, name));
                    }
                    else if (ItemType == ItemType.Array)
                    {
                        string arrayType = $"List<{DecideItemType(jToken.First.First.Value<string>())}>";

                        _settingsObjects[parentName].Add(new SettingModel(arrayType, name));
                    }
                    else
                    {
                        JValue jValue = (JValue)jToken;

                        string itemType = DecideItemType(jValue.ToString());

                        _settingsObjects[parentName].Add(new SettingModel(itemType, name));
                    }
                }
            }
        }

        private (string name, string parentName) GetParentNames(string jPath)
        {
            string name = "";
            string parrentName = "";

            if (!string.IsNullOrEmpty(jPath))
            {
                string[] pathItems = jPath.Split('.');

                if (pathItems.Length > 1)
                {
                    parrentName = pathItems[pathItems.Length - 2];
                }

                name = pathItems[pathItems.Length - 1];
            }

            return (name, parrentName);
        }

        private string DecideItemType(string value)
        {
            if (bool.TryParse(value, out bool isBool))
            {
                return "bool";
            }
            else if (DateTime.TryParse(value, out DateTime isDate))
            {
                return "DateTime";
            }
            else if (int.TryParse(value, out int isInt))
            {
                return "int";
            }
            else
            {
                return "string";
            }
        }

        private JToken ReadJsonFile(string path)
        {
            string jsonText = File.ReadAllText(path);

            JToken res = JToken.Parse(jsonText);

            return res;
        }

        private void OutputModels()
        {
            foreach (var setting in _settingsObjects)
            {
                string className,
                 classNamespace,
                 fileName,
                 folderPath;

                if (string.IsNullOrEmpty(setting.Key))
                {
                    className = rootClassName;
                    classNamespace = rootNamespace + "." + projectName;

                    fileName = $"\\{rootClassName}.cs";
                    folderPath = outputFolder;
                }
                else
                {
                    className = setting.Key;
                    classNamespace = rootNamespace + "." + projectName + "." + entitiesFolder;

                    fileName = $"\\{setting.Key}.cs";
                    folderPath = $"{outputFolder}\\{entitiesFolder}";
                }

                string fullPath = folderPath + fileName;

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (!File.Exists(fullPath))
                {
                    File.Create(fullPath).Dispose();
                }

                string fileBody = GenerateModel(className, classNamespace, setting.Value);

                File.WriteAllText(fullPath, fileBody, Encoding.UTF8);
            }
        }

        private string GenerateModel(string className, string classNamespace, List<SettingModel> settingItems)
        {
            StringBuilder res = new StringBuilder();

            StringBuilder fields = new StringBuilder();
            StringBuilder properties = new StringBuilder();

            res.Append(CodeExamples.Directives)
                .AppendLine()
                .AppendLine($"namespace {classNamespace}")
                .AppendLine("{")
                .AppendLine($"\tpublic class {className}")
                .AppendLine("\t{");

            foreach (var item in settingItems)
            {
                bool needDecrypt = Array.IndexOf(itemsToDecrypt, item.Name) > -1;

                if (needDecrypt)
                {
                    fields.AppendLine($"\t\tprivate {item.Type} {item.Name.ToLower()};");

                    properties.AppendLine($"\t\tpublic {item.Type} {item.Name} {CodeExamples.GetSet_Decrypt(item.Name.ToLower())}").AppendLine();
                }
                else
                {
                    properties.AppendLine($"\t\tpublic {item.Type} {item.Name} {CodeExamples.GetSet}").AppendLine();
                }
            }

            if (fields.Length > 0)
            {
                res.Append(fields).AppendLine();
            }

            res.Append(properties)
                .Remove(res.Length - 2, 2)
                .AppendLine("\t}")
                .AppendLine("}");

            return res.ToString();
        }

        #endregion
    }
}
