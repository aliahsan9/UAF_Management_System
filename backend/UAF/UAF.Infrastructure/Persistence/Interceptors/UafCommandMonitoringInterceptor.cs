using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace UAF.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Emits safe, structured database command telemetry without logging SQL text or parameter values.
/// </summary>
public sealed class UafCommandMonitoringInterceptor : DbCommandInterceptor
{
    private readonly ILogger<UafCommandMonitoringInterceptor> _logger;

    public UafCommandMonitoringInterceptor(ILogger<UafCommandMonitoringInterceptor> logger) =>
        _logger = logger;

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogCompleted(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogCompleted(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogCompleted(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        LogCompleted(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogCompleted(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        LogCompleted(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _logger.LogError(eventData.Exception,
            "Database command failed. CommandType: {CommandType}, DurationMs: {DurationMs}, ParameterCount: {ParameterCount}",
            command.CommandType, eventData.Duration.TotalMilliseconds, command.Parameters.Count);
    }

    public override Task CommandFailedAsync(
        DbCommand command, CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        CommandFailed(command, eventData);
        return Task.CompletedTask;
    }

    private void LogCompleted(DbCommand command, CommandExecutedEventData eventData)
    {
        _logger.LogDebug(
            "Database command completed. CommandType: {CommandType}, DurationMs: {DurationMs}, ParameterCount: {ParameterCount}",
            command.CommandType, eventData.Duration.TotalMilliseconds, command.Parameters.Count);
    }
}