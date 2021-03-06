using System.Linq;

namespace Entitas.CodeGenerator {

    public class ContextsGenerator : ICodeGenerator {

        public string name { get { return "Contexts"; } }
        public bool isEnabledByDefault { get { return true; } }

        const string CONTEXTS_TEMPLATE =
@"public partial class Contexts : Entitas.IContexts {

    public static Contexts sharedInstance {
        get {
            if(_sharedInstance == null) {
                _sharedInstance = new Contexts();
            }

            return _sharedInstance;
        }
        set { _sharedInstance = value; }
    }

    static Contexts _sharedInstance;

    public static void CreateContextObserver(Entitas.IContext context) {
#if(!ENTITAS_DISABLE_VISUAL_DEBUGGING && UNITY_EDITOR)
        if(UnityEngine.Application.isPlaying) {
            var observer = new Entitas.Unity.VisualDebugging.ContextObserver(context);
            UnityEngine.Object.DontDestroyOnLoad(observer.gameObject);
        }
#endif
    }

${contextProperties}

    public Entitas.IContext[] allContexts { get { return new Entitas.IContext [] { ${contextList} }; } }

    public Contexts() {
${contextAssignments}

${contextObservers}

        var postConstructors = System.Linq.Enumerable.Where(
            GetType().GetMethods(),
            method => System.Attribute.IsDefined(method, typeof(Entitas.CodeGenerator.Api.PostConstructorAttribute))
        );

        foreach(var postConstructor in postConstructors) {
            postConstructor.Invoke(this, null);
        }
    }

    public void Reset() {
        var contexts = allContexts;
        for (int i = 0; i < contexts.Length; i++) {
            contexts[i].Reset();
        }
    }
}
";

        const string CONTEXT_PROPERTY_TEMPLATE = @"    public ${ContextName}Context ${contextName} { get; set; }";
        const string CONTEXT_LIST_TEMPLATE = @"${contextName}";
        const string CONTEXT_ASSIGNMENT_TEMPLATE = @"        ${contextName} = new ${ContextName}Context();";
        const string CONTEXT_OBSERVER_TEMPLATE = @"        CreateContextObserver(${contextName});";

        public CodeGenFile[] Generate(CodeGeneratorData[] data) {
            var contextNames = data
                .OfType<ContextData>()
                .Select(d => d.GetContextName())
                .OrderBy(contextName => contextName)
                .ToArray();

            return new[] { new CodeGenFile(
                "Contexts.cs",
                generateContextsClass(contextNames),
                GetType().FullName)
            };
        }

        string generateContextsClass(string[] contextNames) {
            var contextProperties = string.Join("\n", contextNames
                .Select(contextName => CONTEXT_PROPERTY_TEMPLATE
                        .Replace("${ContextName}", contextName)
                        .Replace("${contextName}", contextName.LowercaseFirst())
                       ).ToArray());

            var contextList = string.Join(", ", contextNames
                .Select(contextName => CONTEXT_LIST_TEMPLATE
                        .Replace("${contextName}", contextName.LowercaseFirst())
                       ).ToArray());

            var contextAssignments = string.Join("\n", contextNames
                .Select(contextName => CONTEXT_ASSIGNMENT_TEMPLATE
                        .Replace("${ContextName}", contextName)
                        .Replace("${contextName}", contextName.LowercaseFirst())
                       ).ToArray());

            var contextObservers = string.Join("\n", contextNames
                .Select(contextName => CONTEXT_OBSERVER_TEMPLATE
                        .Replace("${contextName}", contextName.LowercaseFirst())
                       ).ToArray());

            return CONTEXTS_TEMPLATE
                .Replace("${contextProperties}", contextProperties)
                .Replace("${contextList}", contextList)
                .Replace("${contextAssignments}", contextAssignments)
                .Replace("${contextObservers}", contextObservers);
        }
    }
}
