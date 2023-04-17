namespace Funky.Azure.DataTable.Extensions.Commands;

public class CommandResponse<TA, TB>
    where TA : CommandOperation
    where TB : CommandOperation
{
    private CommandResponse(CommandOperation operation) => Operation = operation;

    public CommandOperation Operation { get; }

    public static implicit operator CommandResponse<TA, TB>(TA a) => new(a);

    public static implicit operator CommandResponse<TA, TB>(TB b) => new(b);
}
