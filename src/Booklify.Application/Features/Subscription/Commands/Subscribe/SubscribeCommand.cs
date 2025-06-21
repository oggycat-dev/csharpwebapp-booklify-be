using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Commands.Subscribe;

public record SubscribeCommand(SubscribeRequest Request) : IRequest<Result<SubscribeResponse>>; 