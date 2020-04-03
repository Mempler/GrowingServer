using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CSharp;

namespace GTServ.ItemGenerator
{
    public class XamlProcessor
    {
        private static ItemsDbDecoder _decoder = new ItemsDbDecoder();

        private static string serializeName(string name)
        {
            return name.Replace(" ", "")
                .Replace("-", "_")
                .Replace("'", "_")
                .Replace(":", "_")
                .Replace("!", "_")
                .Replace("#", "_")
                .Replace(".", "_")
                .Replace("?", "_")
                .Replace("&", "_")
                .Replace("(", "_")
                .Replace(")", "_")
                .Replace(",", "_");
        }
        public static void Process()
        {
            foreach (var f in Directory.GetFiles("ClassInformation").Reverse())
            {
                var xamlDocument = XDocument.Load(f);

                var xmlNameSpace = xamlDocument.Root;
                var xmlNameSpaceName = xmlNameSpace?.Attribute("name")?.Value ?? string.Empty;
                if (xmlNameSpace == null)
                    throw new NullReferenceException("Namespace is null!");
                
                var compileUnit = new CodeCompileUnit();
                var nameSpace = new CodeNamespace(xmlNameSpaceName);
                
                foreach (var elX in xmlNameSpace.Elements())
                {
                    switch (elX.Name.LocalName)
                    {
                        case "class":
                        { 
                            var className = elX.Attribute("name")?.Value;
                            var cls = new CodeTypeDeclaration(className);
                            cls.IsClass = true;
                            
                            #region Packer
                            var packer = new CodeMemberMethod();
                            {
                                packer.Name = "Pack";
                                packer.ReturnType = new CodeTypeReference(typeof(byte[]));
                                packer.Attributes = MemberAttributes.Public;

                                var packerMemoryStream =
                                    new CodeVariableDeclarationStatement("using System.IO.MemoryStream", "ms")
                                    {
                                        InitExpression = new CodeObjectCreateExpression("System.IO.MemoryStream")
                                    };

                                {
                                    packer.Statements.Add(packerMemoryStream);

                                    foreach (var elClass in elX.Elements())
                                    {
                                        if (elClass.Attribute("write")?.Value == "False")
                                            continue;
                                        
                                        switch (elClass.Attribute("type")?.Value)
                                        {
                                            case "System.String":
                                            {
                                                var isEncrypted = elClass.Attribute("encrypted")?.Value == "True";
                                                CodeMethodInvokeExpression getBytesFromString;
                                                if (isEncrypted)
                                                    getBytesFromString = new CodeMethodInvokeExpression(
                                                        new CodeThisReferenceExpression(),
                                                        "EncryptName");
                                                else
                                                    getBytesFromString = new CodeMethodInvokeExpression(
                                                        new CodeTypeReferenceExpression("System.Text.Encoding"),
                                                        "ASCII.GetBytes",
                                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                            elClass.Attribute("name")?.Value));

                                                var stringDataVariable =
                                                    new CodeVariableDeclarationStatement("System.Byte[]",
                                                        "var_" + elClass.Attribute("name")?.Value)
                                                    {
                                                        InitExpression = getBytesFromString
                                                    };

                                                packer.Statements.Add(stringDataVariable);

                                                var getByteCount =
                                                    new CodeVariableReferenceExpression(
                                                        "var_" + elClass.Attribute("name")?.Value + ".Length");

                                                var bitConverterGetBytes = new CodeMethodInvokeExpression(
                                                    new CodeTypeReferenceExpression("System.BitConverter"),
                                                    "GetBytes",
                                                    new CodeCastExpression("System.UInt16", getByteCount));

                                                var packerMemoryStreamWriteLength = new CodeMethodInvokeExpression(
                                                    new CodeVariableReferenceExpression("ms"),
                                                    "Write", bitConverterGetBytes);

                                                var packerMemoryStreamWrite = new CodeMethodInvokeExpression(
                                                    new CodeVariableReferenceExpression("ms"),
                                                    "Write",
                                                    new CodeVariableReferenceExpression(
                                                        "var_" + elClass.Attribute("name")?.Value));

                                                packer.Statements.Add(packerMemoryStreamWriteLength);
                                                packer.Statements.Add(packerMemoryStreamWrite);
                                                break;
                                            }
                                            case "System.Byte[]":
                                            {
                                                var packerMemoryStreamWrite = new CodeMethodInvokeExpression(
                                                    new CodeVariableReferenceExpression("ms"),
                                                    "Write",
                                                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                        elClass.Attribute("name")?.Value));

                                                packer.Statements.Add(packerMemoryStreamWrite);
                                                break;
                                            }
                                            case "System.Drawing.Color":
                                            {
                                                var packerColorToArgb = new CodeMethodInvokeExpression(
                                                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                        elClass.Attribute("name")?.Value),
                                                    "ToArgb");

                                                var bitConverterGetBytes = new CodeMethodInvokeExpression(
                                                    new CodeTypeReferenceExpression("System.BitConverter"),
                                                    "GetBytes",
                                                    packerColorToArgb);

                                                var packerMemoryStreamWrite = new CodeMethodInvokeExpression(
                                                    new CodeVariableReferenceExpression("ms"),
                                                    "Write", bitConverterGetBytes);

                                                packer.Statements.Add(packerMemoryStreamWrite);
                                                break;
                                            }
                                            case "System.TimeSpan":
                                            {
                                                var packerTimeSpanTotalSeconds =
                                                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                        elClass.Attribute("name")?.Value + ".TotalMilliseconds");

                                                var bitConverterGetBytes = new CodeMethodInvokeExpression(
                                                    new CodeTypeReferenceExpression("System.BitConverter"),
                                                    "GetBytes",
                                                    packerTimeSpanTotalSeconds);

                                                var packerMemoryStreamWrite = new CodeMethodInvokeExpression(
                                                    new CodeVariableReferenceExpression("ms"),
                                                    "Write", bitConverterGetBytes);

                                                packer.Statements.Add(packerMemoryStreamWrite);
                                                break;
                                            }
                                            case "System.SByte":
                                            case "System.Byte":
                                            {
                                                var packerMemoryStreamWrite = new CodeMethodInvokeExpression(
                                                    new CodeVariableReferenceExpression("ms"),
                                                    "WriteByte", new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                        elClass.Attribute("name")?.Value));

                                                packer.Statements.Add(packerMemoryStreamWrite);
                                                
                                                break;
                                            }
                                            default:
                                            {
                                                if (elClass.Attribute("then")?.Value == "Generated.ItemsDb.Enums.ItemId")
                                                {
                                                    var bitConverterGetBytes = new CodeMethodInvokeExpression(
                                                        new CodeTypeReferenceExpression("System.BitConverter"),
                                                        "GetBytes",
                                                        new CodeCastExpression(elClass.Attribute("type")?.Value,  new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                            elClass.Attribute("name")?.Value))
                                                    );
                                            
                                                    var packerMemoryStreamWrite = new CodeMethodInvokeExpression(
                                                        new CodeVariableReferenceExpression("ms"),
                                                        "Write", bitConverterGetBytes);
                                            
                                                    packer.Statements.Add(packerMemoryStreamWrite);
                                                }
                                                else
                                                {
                                                    var bitConverterGetBytes = new CodeMethodInvokeExpression(
                                                        new CodeTypeReferenceExpression("System.BitConverter"),
                                                        "GetBytes",
                                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                            elClass.Attribute("name")?.Value));

                                                    var packerMemoryStreamWrite = new CodeMethodInvokeExpression(
                                                        new CodeVariableReferenceExpression("ms"),
                                                        "Write", bitConverterGetBytes);

                                                    packer.Statements.Add(packerMemoryStreamWrite);
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }

                                var packerMemoryStreamToArray = new CodeMethodInvokeExpression(
                                    new CodeVariableReferenceExpression("ms"),
                                    "ToArray");

                                packer.Statements.Add(new CodeMethodReturnStatement(packerMemoryStreamToArray));
                            }
                            cls.Members.Add(packer);
                            #endregion
                            Console.WriteLine("Generate Packer in {0}", xmlNameSpaceName);
                            
                            #region Encryption
                            var nameEncryptor = new CodeMemberMethod();
                            {
                                nameEncryptor.Name = "EncryptName";
                                nameEncryptor.ReturnType = new CodeTypeReference("System.Byte[]");
                                
                                nameEncryptor.Statements.Add(
                                    new CodeSnippetStatement("            const string secret = \"PBG892FXX982ABC*\";"));
                                nameEncryptor.Statements.Add(
                                    new CodeSnippetStatement("            var encrypted = new byte[Name.Length];"));
                                nameEncryptor.Statements.Add(
                                    new CodeSnippetStatement("            for (var i = 0; i < Name.Length; i++)"));
                                nameEncryptor.Statements.Add(
                                    new CodeSnippetStatement("                encrypted[i] = (byte) (Name[i] ^ (secret[(i + (int) ItemId) % secret.Length]));"));
                                nameEncryptor.Statements.Add(
                                    new CodeSnippetStatement("            return encrypted;"));
                            }
                            
                            cls.Members.Add(nameEncryptor);
                            #endregion
                            Console.WriteLine("Generate Name Encryption in {0}", xmlNameSpaceName);
                            
                            #region Properties
                            
                            foreach (var elClass in elX.Elements())
                            {
                                var nm = elClass.Attribute("name")?.Value ?? string.Empty;

                                var t = elClass.Attribute("type")?.Value ?? "System.Object";
                                var then = elClass.Attribute("then")?.Value ?? t;
                                
                                Console.WriteLine("Generate Property {1} width Type {2} in {0}", xmlNameSpaceName, nm, then);
                                
                                var prop = new CodeMemberProperty {Attributes = MemberAttributes.Public};
                                prop.GetStatements.Add( new CodeMethodReturnStatement( new CodeDefaultValueExpression(new CodeTypeReference(then)) ) );

                                prop.Name = nm;
                                prop.Type = new CodeTypeReference(then);

                                cls.Members.Add(prop);
                            }

                            #endregion
                            
                            nameSpace.Types.Add(cls);
                            
                            Console.WriteLine("Decoding Items.dat...");
                            _decoder.Decode(elX, "items.dat");
                        } break;

                        case "enum":
                        {
                            var className = elX.Attribute("name")?.Value;
                            var cls = new CodeTypeDeclaration(className) {IsEnum = true};

                            foreach (var item in _decoder.Data)
                            {
                                var field = new CodeMemberField(className, serializeName("i"+item["ItemId"]+"_"+item["Name"]));
                                
                                Console.WriteLine("Generate BlockId {0}", field.Name);
                                
                                field.InitExpression = new CodeSnippetExpression($"{ (int) item["ItemId"] }");

                                cls.Members.Add(field);
                            }
                            nameSpace.Types.Add(cls);

                        } break;
                        
                        default:
                            throw new ArgumentOutOfRangeException(nameof(elX), "Unknown type!");
                    }
                }
                
                nameSpace.Imports.Add(new CodeNamespaceImport("System.Linq"));
                compileUnit.Namespaces.Add(nameSpace);
                
                var provider = new CSharpCodeProvider();
                
                var fileName = nameSpace.Name.Split(".").Last();
                var path = "../GTServ.Generated/" + string.Join("/", nameSpace.Name.Split(".").Where(x => x != fileName));

                Directory.CreateDirectory(path);
                
                using var ms = File.Open(path + "/" + fileName + ".cs", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using var sw = new StreamWriter(ms);
                using var tw = new IndentedTextWriter(sw, "    ");

                // Generate source code using the code provider.
                provider.GenerateCodeFromCompileUnit(compileUnit, tw,
                    new CodeGeneratorOptions());
            }
            
            foreach (var item in _decoder.Data)
            {
                var compileUnit = new CodeCompileUnit();
                
                var clsName = "I" + item["ItemId"] + "_" + serializeName((string)item["Name"]);
                Console.WriteLine("Generate Item Class " + clsName);
                var nameSpace = new CodeNamespace("Generated.ItemsDb.Items");

                var itemCls = new CodeTypeDeclaration(clsName);
                itemCls.BaseTypes.Add("Generated.ItemsDb.Item.Item");
                
                foreach (var (name, value) in item)
                {
                    #region Props
                    
                    var prop = new CodeMemberProperty {Attributes = MemberAttributes.Public};
                    
                    switch (value)
                    {
                        case TimeSpan span:
                        {
                            var primitiveExpression = new CodePrimitiveExpression(span.Ticks);
                        
                            prop.GetStatements.Add( new CodeMethodReturnStatement(new CodeObjectCreateExpression( span.GetType(), primitiveExpression)));
                            break;
                        }
                        case Color color:
                        {
                            var primitiveExpression = new CodePrimitiveExpression(color.ToArgb());
                        
                            var colFromArgb = new CodeMethodInvokeExpression(
                                new CodeVariableReferenceExpression("System.Drawing.Color"),
                                "FromArgb", primitiveExpression);
                        
                            prop.GetStatements.Add( new CodeMethodReturnStatement(colFromArgb) );
                            break;
                        }
                        case byte[] bytes:
                        {
                            var primitiveExpressions = (from v in bytes select new CodePrimitiveExpression(v)).ToArray();

                            var primitiveExpression = new CodeArrayCreateExpression(bytes.GetType(), primitiveExpressions );
                        
                            prop.GetStatements.Add( new CodeMethodReturnStatement(primitiveExpression) );
                            break;
                        }
                        default:
                        {
                            var primitiveExpression = new CodePrimitiveExpression(value);
                            prop.GetStatements.Add(name != "ItemId"
                                ? new CodeMethodReturnStatement(new CodeCastExpression(value.GetType(),
                                    primitiveExpression))
                                : new CodeMethodReturnStatement(new CodeCastExpression("Generated.ItemsDb.Enums.ItemId",
                                    primitiveExpression)));
                            break;
                        }
                    }
                    
                    prop.Attributes = MemberAttributes.Public | MemberAttributes.Override;
                    prop.Name = name;
                    
                    prop.Type = name != "ItemId" ?
                        new CodeTypeReference(value.GetType()) :
                        new CodeTypeReference("Generated.ItemsDb.Enums.ItemId");

                    itemCls.Members.Add(prop);

                    #endregion
                }

                nameSpace.Types.Add(itemCls);
                compileUnit.Namespaces.Add(nameSpace);
                
                var provider = new CSharpCodeProvider();
                
                var fileName = clsName;
                var path =  "../GTServ.Generated/" + string.Join("/", nameSpace.Name.Split(".").Where(x => x != fileName));

                Directory.CreateDirectory(path);
                
                using var ms = File.Open(path + "/" + fileName + ".cs", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using var sw = new StreamWriter(ms);
                using var tw = new IndentedTextWriter(sw, "    ");

                // Generate source code using the code provider.
                provider.GenerateCodeFromCompileUnit(compileUnit, tw,
                    new CodeGeneratorOptions());
            }
        }
    }
}