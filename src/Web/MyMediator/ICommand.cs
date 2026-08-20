using System.Diagnostics.CodeAnalysis;

namespace Web.MyMediator;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces",
	Justification = "Marker interface used as a generic constraint by the mediator pipeline; it has no members by design.")]
public interface ICommand<TResponse> : IRequest<TResponse>
{
}
