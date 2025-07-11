using Booklify.Application.Common.DTOs.ReadingProgress;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.ReadingProgress.Commands.StartReading;

public record TrackingReadingSessionCommand(TrackingReadingSessionRequest Request) : IRequest<Result<TrackingSessionResponse>>;