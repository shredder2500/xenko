using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.SpriteStudio.Runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SiliconStudio.Paradox.SpriteStudio.Offline
{
    internal class SpriteStudioXmlImport
    {
        private static void FillNodeData(XNamespace nameSpace, XContainer part, List<SpriteStudioCell> cells, out NodeAnimationData nodeData)
        {
            nodeData = new NodeAnimationData();

            var attribs = part.Descendants(nameSpace + "attribute");
            foreach (var attrib in attribs)
            {
                var tag = attrib.Attributes("tag").First().Value;
                var keys = attrib.Descendants(nameSpace + "key");

                switch (tag)
                {
                    case "CELL":
                        {
                            var keyValues = new List<Dictionary<string, string>>();
                            foreach (var key in keys)
                            {
                                var values = new Dictionary<string, string>();
                                var cellName = key.Descendants(nameSpace + "value").First().Descendants(nameSpace + "name").First().Value;
                                var index = 0;
                                var realIndex = -1;
                                foreach (var cell in cells)
                                {
                                    if (cell.Name == cellName)
                                    {
                                        realIndex = index;
                                    }
                                    index++;
                                }
                                values.Add("time", key.Attribute("time").Value);
                                values.Add("curve", key.Attribute("ipType") != null ? key.Attribute("ipType").Value : "linear");
                                values.Add("value", realIndex.ToString());

                                keyValues.Add(values);
                            }
                            nodeData.Data.Add(tag, keyValues);
                        }
                        break;

                    default:
                        {
                            var values = keys.Where(key => key.Descendants(nameSpace + "value").FirstOrDefault() != null).Select(key => new Dictionary<string, string>
                            {
                                { "time", key.Attribute("time").Value },
                                { "curve", key.Attribute("ipType") != null ? key.Attribute("ipType").Value : "linear" },
                                { "value", key.Descendants(nameSpace + "value").First().Value }
                            }).ToList();
                            nodeData.Data.Add(tag, values);
                        }
                        break;
                }
            }
        }

        public static bool ParseAnimations(UFile file, List<SpriteStudioAnim> animations)
        {
            var textures = new List<UFile>();
            var cells = new List<SpriteStudioCell>();
            if (!ParseCellMaps(file, textures, cells)) return false;

            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            var anims = xmlDoc.Descendants(nameSpace + "anime");
            foreach (var animXml in anims)
            {
                var animName = animXml.Descendants(nameSpace + "name").First().Value;

                int fps, frameCount;
                if (!int.TryParse(animXml.Descendants(nameSpace + "fps").First().Value, out fps)) continue;
                if (!int.TryParse(animXml.Descendants(nameSpace + "frameCount").First().Value, out frameCount)) continue;

                var anim = new SpriteStudioAnim
                {
                    Name = animName,
                    Fps = fps,
                    FrameCount = frameCount
                };

                var animParts = animXml.Descendants(nameSpace + "partAnime");
                foreach (var animPart in animParts)
                {
                    NodeAnimationData data;
                    FillNodeData(nameSpace, animPart, cells, out data);
                    anim.NodesData.Add(animPart.Descendants(nameSpace + "partName").First().Value, data);
                }

                animations.Add(anim);
            }

            return true;
        }

        public static bool ParseModel(UFile file, List<SpriteStudioNode> nodes, out string modelName)
        {
            modelName = string.Empty;

            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            modelName = xmlDoc.Descendants(nameSpace + "name").First().Value;

            var model = xmlDoc.Descendants(nameSpace + "Model").First();
            var modelNodes = model.Descendants(nameSpace + "value");
            foreach (var xmlNode in modelNodes)
            {
                var nodeName = xmlNode.Descendants(nameSpace + "name").First().Value;
                var isNull = xmlNode.Descendants(nameSpace + "type").First().Value == "null";
                int nodeId, parentId;
                if (!int.TryParse(xmlNode.Descendants(nameSpace + "arrayIndex").First().Value, out nodeId)) continue;
                if (!int.TryParse(xmlNode.Descendants(nameSpace + "parentIndex").First().Value, out parentId)) continue;

                var blendingName = xmlNode.Descendants(nameSpace + "alphaBlendType").First().Value;

                var node = new SpriteStudioNode
                {
                    Name = nodeName,
                    Id = nodeId,
                    ParentId = parentId,
                    IsNull = isNull
                };

                switch (blendingName)
                {
                    case "mix":
                        node.AlphaBlending = SpriteStudioAlphaBlending.Mix;
                        break;

                    case "add":
                        node.AlphaBlending = SpriteStudioAlphaBlending.Addition;
                        break;

                    case "mul":
                        node.AlphaBlending = SpriteStudioAlphaBlending.Multiplication;
                        break;

                    case "sub":
                        node.AlphaBlending = SpriteStudioAlphaBlending.Subtraction;
                        break;
                }

                nodes.Add(node);
            }

            return true;
        }

        public static bool ParseCellMaps(UFile file, List<UFile> textures, List<SpriteStudioCell> cells)
        {
            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            var cellMaps = xmlDoc.Descendants(nameSpace + "cellmapNames").Descendants(nameSpace + "value");

            foreach (var cellMap in cellMaps)
            {
                var mapFile = UPath.Combine(file.GetFullDirectory(), new UFile(cellMap.Value));
                var cellDoc = XDocument.Load(mapFile);
                if (cellDoc.Root == null) return false;

                var cnameSpace = cellDoc.Root.Name.Namespace;

                var cellNodes = cellDoc.Descendants(nameSpace + "cell");
                foreach (var cellNode in cellNodes)
                {
                    var cell = new SpriteStudioCell
                    {
                        Name = cellNode.Descendants(cnameSpace + "name").First().Value,
                        TextureIndex = textures.Count
                    };
   
                    var posData = cellNode.Descendants(nameSpace + "pos").First().Value;
                    var posValues = Regex.Split(posData, "\\s+");
                    var sizeData = cellNode.Descendants(nameSpace + "size").First().Value;
                    var sizeValues = Regex.Split(sizeData, "\\s+");
                    cell.Rectangle = new RectangleF(float.Parse(posValues[0]), float.Parse(posValues[1]), float.Parse(sizeValues[0]), float.Parse(sizeValues[1]));

                    var pivotData = cellNode.Descendants(nameSpace + "pivot").First().Value;
                    var pivotValues = Regex.Split(pivotData, "\\s+");
                    cell.Pivot = new Vector2((float.Parse(pivotValues[0]) + 0.5f) * cell.Rectangle.Width, (-float.Parse(pivotValues[1]) + 0.5f) * cell.Rectangle.Height);

                    cells.Add(cell);
                }

                var textPath = cellDoc.Descendants(nameSpace + "imagePath").First().Value;

                textures.Add(UPath.Combine(file.GetFullDirectory(), new UFile(textPath)));
            }

            return true;
        }

        public static bool SanityCheck(UFile file)
        {
            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            var cellMaps = xmlDoc.Descendants(nameSpace + "cellmapNames").Descendants(nameSpace + "value").ToList();
            return cellMaps.Select(cellMap => UPath.Combine(file.GetFullDirectory(), new UFile(cellMap.Value))).All(fileName => File.Exists(fileName.ToWindowsPath()));
        }
    }
}