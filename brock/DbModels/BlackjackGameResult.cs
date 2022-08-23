using brock.Blackjack;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

public class BlackjackGameResult
{
	public int Id { get; set; }
	public string Username { get; set; }
	public float Wager { get; set; }
	public string PlayerHand { get; set; }
	public int PlayerScore { get; set; }
    public string DealerHand { get; set; }
	public int DealerScore { get; set; }
    [ForeignKey("GameStateEnum")] public int GameStateEnumId { get; set; }
	public virtual GameStateEnum GameStateEnum { get; set; }
	public DateTime CreatedDate { get; set; }
	public bool Ignore { get; set; }
}

public class GameStateEnum
{
	public int Id { get; set; }
	public string GameState { get; set; }
}

public class BlackjackContext : DbContext
{
	public BlackjackContext(string connectionString)
	{
		Database.Connection.ConnectionString = connectionString;
	}
	public DbSet<BlackjackGameResult> GameResults { get; set; }
	public DbSet<GameStateEnum> GameStateEnum { get; set; }

	protected override void OnModelCreating(DbModelBuilder modelBuilder)
	{
		Database.SetInitializer<BlackjackContext>(null);
		modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
		base.OnModelCreating(modelBuilder);
	}
}