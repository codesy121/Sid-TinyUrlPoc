namespace TinyUrl.Api.Domain;

public interface ICodeGenerator
{
    string Generate(int length);
}
