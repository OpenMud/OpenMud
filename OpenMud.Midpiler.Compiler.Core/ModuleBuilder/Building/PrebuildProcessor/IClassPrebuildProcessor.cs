namespace OpenMud.Mudpiler.Compiler.Core.ModuleBuilder.Building.PrebuildProcessor;

public interface IClassPrebuildProcessor
{
    void Process(string fullName, IDreamMakerSymbolResolver cls);
}