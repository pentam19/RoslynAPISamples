using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// dotnet add package Microsoft.CodeAnalysis.CSharp --version 3.10.0
namespace RoslynAPISample01
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            using(FileStream fs = new FileStream(args[0],FileMode.Open)){
                // 解析対象のソースファイルを読み込む
                StreamReader sr = new StreamReader(fs);
                string Text = sr.ReadToEnd();

                //Console.WriteLine(Text);
                SyntaxTree tr = CSharpSyntaxTree.ParseText(Text);
                SyntaxNode node = tr.GetRoot();
                // これでNodeに変換できたので好きなように扱えるようになった。

                // 全ノードを巡回して情報を書き出す。
                AllNodeWriter writer = new AllNodeWriter();
                writer.Visit(node);

                Console.WriteLine("-----------");
                // -----
                CompilationUnitSyntax root = tr.GetCompilationUnitRoot();
                Console.WriteLine($"The tree is a {root.Kind()} node.");
                Console.WriteLine($"The tree has {root.Members.Count} elements in it.");
                Console.WriteLine($"The tree has {root.Usings.Count} using statements. They are:");
                // usingを取得
                foreach (UsingDirectiveSyntax element in root.Usings)
                    Console.WriteLine($"\t{element.Name}");

                // ネームスペースが取れる
                Console.WriteLine("-----------");
                MemberDeclarationSyntax firstMember = root.Members[0];
                Console.WriteLine($"The first member is a {firstMember.Kind()}.");
                // The first member is a NamespaceDeclaration.
                var declaration = (NamespaceDeclarationSyntax)firstMember;
                /*
                Console.WriteLine("-----------");
                foreach (MemberDeclarationSyntax member in root.Members)
                    Console.WriteLine($"The member is a {member.Kind()}.");
                 */
                Console.WriteLine("-----------");
                Console.WriteLine($"There are {declaration.Members.Count} members declared in this namespace.");
                // ネームスペースの中にクラス定義がある
                Console.WriteLine($"The first member is a {declaration.Members[0].Kind()}.");
                // The first member is a ClassDeclaration.

                Console.WriteLine("-----------");
                var programDeclaration = (ClassDeclarationSyntax)declaration.Members[0];
                // クラスの中にメソッドが３つ
                Console.WriteLine($"There are {programDeclaration.Members.Count} members declared in the {programDeclaration.Identifier} class.");
                Console.WriteLine($"The first member is a {programDeclaration.Members[0].Kind()}.");

                // maimメソッド
                var mainDeclaration = (MethodDeclarationSyntax)programDeclaration.Members[0];

                // 戻り値、メソッドのパラメータ、パラメータの型、ソースコード
                Console.WriteLine("-----------");
                Console.WriteLine($"The return type of the {mainDeclaration.Identifier} method is {mainDeclaration.ReturnType}.");
                Console.WriteLine($"The method has {mainDeclaration.ParameterList.Parameters.Count} parameters.");
                foreach (ParameterSyntax item in mainDeclaration.ParameterList.Parameters)
                    Console.WriteLine($"The type of the {item.Identifier} parameter is {item.Type}.");
                Console.WriteLine($"The body text of the {mainDeclaration.Identifier} method follows:");
                Console.WriteLine(mainDeclaration.Body.ToFullString());
                // パラメータ
                var argsParameter = mainDeclaration.ParameterList.Parameters[0];
                Console.WriteLine($"argsParameter: {argsParameter}");
                // クエリを使って取得
                var firstParameters = from methodDeclaration in root.DescendantNodes()
                                                        .OfType<MethodDeclarationSyntax>()
                                    where methodDeclaration.Identifier.ValueText == "Main"
                                    select methodDeclaration.ParameterList.Parameters.First();
                var argsParameter2 = firstParameters.Single();
                Console.WriteLine("argsParameter == get query argsParameter2 ?");
                Console.WriteLine(argsParameter == argsParameter2);

                /*
                foreach (MethodDeclarationSyntax member in programDeclaration.Members)
                {
                    Console.WriteLine($" The member is a {member.Kind()}.");
                    printMethodDiclaration(member);
                }
                 */
                const string programText =
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TopLevel
{
    using Microsoft;
    using System.ComponentModel;

    namespace Child1
    {
        using Microsoft.Win32;
        using System.Runtime.InteropServices;

        class Foo
        {
            static void Main(string[] args)
            {
                string str = getVal();
                Console.WriteLine(str.Length);
            }

            static string getVal()
            {
                string result = ""abc"";
                return result;
            }
        }
    }

    namespace Child2
    {
        using System.CodeDom;
        using Microsoft.CSharp;

        class Bar { }
    }
}";
                SyntaxTree tree2 = CSharpSyntaxTree.ParseText(programText);
                CompilationUnitSyntax root2 = tree2.GetCompilationUnitRoot();
                Console.WriteLine("================");
                var collector = new UsingCollector();
                // System以外のusingを取ってくる
                collector.Visit(root2);
                foreach (var directive in collector.Usings)
                {
                    Console.WriteLine(directive.Name);
                }
                Console.WriteLine("-----------");
                // 変数の代入文
                foreach (var directive in collector.Variables)
                {
                    Console.WriteLine(directive.ToFullString());
                    Console.WriteLine(directive.GetLocation());
                    /*
                    foreach (var ancestor in directive.Ancestors())
                    {
                        Console.WriteLine(ancestor.ToString());
                    }
                     */
                }
                // 関数呼び出し式
                foreach (var directive in collector.InvocationExpression)
                {
                    Console.WriteLine(directive.ToFullString());
                    Console.WriteLine(directive.GetLocation());
                }
            }
        }
        
        static void printMethodDiclaration(MethodDeclarationSyntax declaration)
        {
            Console.WriteLine("-----------");
            Console.WriteLine($"The return type of the {declaration.Identifier} method is {declaration.ReturnType}.");
            Console.WriteLine($"The method has {declaration.ParameterList.Parameters.Count} parameters.");
            foreach (ParameterSyntax item in declaration.ParameterList.Parameters)
                Console.WriteLine($"The type of the {item.Identifier} parameter is {item.Type}.");
            Console.WriteLine($"The body text of the {declaration.Identifier} method follows:");
            Console.WriteLine(declaration.Body.ToFullString());
        }

        // ノードの巡回にはSyntaxWalkerを利用する。
        public class AllNodeWriter : SyntaxWalker
        {
            public override void Visit(SyntaxNode node)
            {
                Console.WriteLine("Visit: " + node.Kind());
                base.Visit(node);
            }
        }

        class UsingCollector : CSharpSyntaxWalker
        {
            // 収集する名前空間ノードを保持する記憶域
            public ICollection<UsingDirectiveSyntax> Usings { get; } = new List<UsingDirectiveSyntax>();
            public ICollection<VariableDeclarationSyntax> Variables { get; } = new List<VariableDeclarationSyntax>();
            public ICollection<InvocationExpressionSyntax> InvocationExpression { get; } = new List<InvocationExpressionSyntax>();

            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                Console.WriteLine($"\tVisitUsingDirective called with {node.Name}.");
                if (node.Name.ToString() != "System" &&
                    !node.Name.ToString().StartsWith("System."))
                {
                    Console.WriteLine($"\t\tSuccess. Adding {node.Name}.");
                    this.Usings.Add(node);
                }
            }

            public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                this.Variables.Add(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                this.InvocationExpression.Add(node);
            }
            /*
            public override void Visit(SyntaxNode node)
            {
                Console.WriteLine("Visit: " + node.Kind());
            }
             */
        }
    }
}
