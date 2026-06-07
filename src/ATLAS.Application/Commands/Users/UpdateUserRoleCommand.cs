using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands
{ 

    public class UpdateUserRoleCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, bool>
    {
        private readonly IUserRepository _repository;

        public UpdateUserRoleCommandHandler(IUserRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(request.UserId, cancellationToken);
            
            if (user == null)
                return false;
            
            user.ChangeRole((UserRole)Enum.Parse(typeof(UserRole), request.Role));
            await _repository.UpdateAsync(user, cancellationToken);
            return true;
        }
    }
}
