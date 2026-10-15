namespace GamePortal.Application.Characters;

public sealed record CharacterDto(int ServerId, string ServerName, long CharacterId, string Name, int Level);
