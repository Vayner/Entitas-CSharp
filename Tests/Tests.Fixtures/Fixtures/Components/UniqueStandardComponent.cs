using Entitas;
using Entitas.CodeGenerator.Api;

[Context("Test"), Unique]
public sealed class UniqueStandardComponent : IComponent {
    public string value;
}
