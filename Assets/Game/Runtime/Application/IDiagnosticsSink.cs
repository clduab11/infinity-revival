namespace Praxen.Game.Application
{
    public interface IDiagnosticsSink
    {
        void Record(in DiagnosticEvent diagnosticEvent);
    }
}
