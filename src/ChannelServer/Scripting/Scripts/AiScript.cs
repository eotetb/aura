// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills;
using Aura.Channel.Skills.Base;
using Aura.Channel.Skills.Combat;
using Aura.Channel.Skills.Life;
using Aura.Channel.Skills.Magic;
using Aura.Channel.World;
using Aura.Channel.World.Entities;
using Aura.Data;
using Aura.Mabi;
using Aura.Mabi.Const;
using Aura.Shared.Network;
using Aura.Shared.Scripting.Scripts;
using Aura.Shared.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Aura.Channel.Scripting.Scripts
{
	// TODO: Rewrite into the new tree design before we make more
	//   of a mess out of this than necessary.
	public abstract class AiScript : IScript, IDisposable
	{
		// Official heartbeat while following a target seems
		// to be about 100-200ms?

		protected int MinHeartbeat = 50; // ms
		protected int IdleHeartbeat = 250; // ms
		protected int AggroHeartbeat = 50; // ms

		// Maintenance
		protected Timer _heartbeatTimer;
		protected int _heartbeat;
		protected double _timestamp;
		protected DateTime _lastBeat;
		protected bool _active;
		protected DateTime _minRunTime;
		private bool _inside = false;
		private int _stuckTestCount = 0;

		protected Random _rnd;
		protected AiState _state;
		protected IEnumerator _curAction;
		protected Creature _newAttackable;

		protected Dictionary<AiState, Dictionary<AiEvent, Dictionary<SkillId, Func<IEnumerable>>>> _reactions;

		protected Dictionary<int, int> _summons = new Dictionary<int, int>();

		// Heartbeat cache
		protected IList<Creature> _playersInRange;

		// Settings
		protected int _aggroRadius, _aggroMaxRadius;
		protected int _visualRadius;
		protected double _visualRadian;
		protected TimeSpan _alertDelay, _aggroDelay, _hateBattleStanceDelay, _hateOverTimeDelay;
		protected DateTime _awareTime, _alertTime;
		protected AggroLimit _aggroLimit;
		protected Dictionary<string, string> _hateTags, _loveTags, _doubtTags;
		protected bool _hatesBattleStance;
		protected int _maxDistanceFromSpawn;

		// Misc
		private int _switchRandomN, _switchRandomM;

		/// <summary>
		/// Creature controlled by AI.
		/// </summary>
		public Creature Creature { get; protected set; }

		/// <summary>
		/// List of random phrases
		/// </summary>
		public List<string> Phrases { get; protected set; }

		/// <summary>
		/// Returns whether the AI is currently active.
		/// </summary>
		public bool Active { get { return _active; } }

		/// <summary>
		/// Returns state of the AI
		/// </summary>
		public AiState State { get { return _state; } }

		protected AiScript()
		{
			this.Phrases = new List<string>();

			_lastBeat = DateTime.MinValue;
			_heartbeat = IdleHeartbeat;
			_heartbeatTimer = new Timer(this.Heartbeat, null, -1, -1);

			_rnd = new Random(RandomProvider.Get().Next());
			_reactions = new Dictionary<AiState, Dictionary<AiEvent, Dictionary<SkillId, Func<IEnumerable>>>>();
			_reactions[AiState.Idle] = new Dictionary<AiEvent, Dictionary<SkillId, Func<IEnumerable>>>();
			_reactions[AiState.Aware] = new Dictionary<AiEvent, Dictionary<SkillId, Func<IEnumerable>>>();
			_reactions[AiState.Alert] = new Dictionary<AiEvent, Dictionary<SkillId, Func<IEnumerable>>>();
			_reactions[AiState.Aggro] = new Dictionary<AiEvent, Dictionary<SkillId, Func<IEnumerable>>>();
			_reactions[AiState.Love] = new Dictionary<AiEvent, Dictionary<SkillId, Func<IEnumerable>>>();

			_state = AiState.Idle;
			_aggroRadius = 500;
			_aggroMaxRadius = 3000;
			_visualRadius = 900;
			_visualRadian = 90;
			_alertDelay = TimeSpan.FromMilliseconds(6000);
			_aggroDelay = TimeSpan.FromMilliseconds(500);
			_hateOverTimeDelay = TimeSpan.FromDays(365);
			_hateBattleStanceDelay = TimeSpan.FromMilliseconds(3000);
			_hateTags = new Dictionary<string, string>();
			_loveTags = new Dictionary<string, string>();
			_doubtTags = new Dictionary<string, string>();

			_maxDistanceFromSpawn = 3000;

			_aggroLimit = AggroLimit.One;
		}

		/// <summary>
		/// Disables heartbeat timer.
		/// </summary>
		public void Dispose()
		{
			_heartbeatTimer.Change(-1, -1);
			_heartbeatTimer.Dispose();
			_heartbeatTimer = null;
		}

		/// <summary>
		/// Called when script is initialized after loading it.
		/// </summary>
		/// <returns></returns>
		public bool Init()
		{
			var attr = this.GetType().GetCustomAttribute<AiScriptAttribute>();
			if (attr == null)
			{
				Log.Error("AiScript.Init: Missing AiScript attribute.");
				return false;
			}

			foreach (var name in attr.Names)
				ChannelServer.Instance.ScriptManager.AiScripts.Add(name, this.GetType());

			return true;
		}

		/// <summary>
		/// Starts AI
		/// </summary>
		public void Activate(double minRunTime)
		{
			if (!_active && _heartbeatTimer != null)
			{
				_active = true;
				_minRunTime = DateTime.Now.AddMilliseconds(minRunTime);
				_heartbeatTimer.Change(_heartbeat, _heartbeat);
			}
		}

		/// <summary>
		/// Pauses AI
		/// </summary>
		public void Deactivate()
		{
			if (_active && _heartbeatTimer != null)
			{
				_active = false;
				_curAction = null;
				_heartbeatTimer.Change(-1, -1);
			}
		}

		/// <summary>
		/// Sets AI's creature.
		/// </summary>
		/// <param name="creature"></param>
		public void Attach(Creature creature)
		{
			this.Creature = creature;
			this.Creature.Finish += OnDeath;
		}

		/// <summary>
		/// Unsets AI's creature.
		/// </summary>
		/// <param name="creature"></param>
		public void Detach()
		{
			var npc = this.Creature as NPC;
			if (npc == null || npc.AI == null)
				return;

			npc.AI.Dispose();
			npc.Finish -= OnDeath;
			npc.AI = null;
			this.Creature = null;
		}

		/// <summary>
		/// Main "loop"
		/// </summary>
		/// <param name="state"></param>
		private void Heartbeat(object state)
		{
			if (this.Creature == null || this.Creature.Region == Region.Limbo)
				return;

			// Skip tick if the previous one is still on.
			if (_inside)
			{
				if (++_stuckTestCount == 10)
					Log.Warning("AiScript.Heartbeat: {0} stuck?", this.GetType().Name);
				return;
			}

			_inside = true;
			_stuckTestCount = 0;
			try
			{
				var now = this.UpdateTimestamp();
				var pos = this.Creature.GetPosition();

				// Stop if no players in range
				_playersInRange = this.Creature.Region.GetPlayersInRange(pos);
				if (_playersInRange.Count == 0 && now > _minRunTime)
				{
					this.Deactivate();
					this.Reset();
					return;
				}

				if (this.Creature.IsDead)
					return;

				this.SelectState();

				// Stop and clear if stunned
				if (this.Creature.IsStunned)
				{
					// Clearing causes it to run aggro from beginning again
					// and again, this should probably be moved to the take
					// damage "event"?
					//this.Clear();
					return;
				}

				// Select and run state
				var prevAction = _curAction;
				if (_curAction == null || !_curAction.MoveNext())
				{
					// If action is switched on the last iteration we end up
					// here, with a new action, which would be overwritten
					// with a default right away without this check.
					if (_curAction == prevAction)
					{
						switch (_state)
						{
							default:
							case AiState.Idle: this.SwitchAction(Idle); break;
							case AiState.Alert: this.SwitchAction(Alert); break;
							case AiState.Aggro: this.SwitchAction(Aggro); break;
							case AiState.Love: this.SwitchAction(Love); break;
						}

						_curAction.MoveNext();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Exception in {0}", this.GetType().Name);
			}
			finally
			{
				_inside = false;
			}
		}

		/// <summary>
		/// Updates timestamp and returns DateTime.Now.
		/// </summary>
		/// <returns></returns>
		private DateTime UpdateTimestamp()
		{
			var now = DateTime.Now;
			_timestamp += (now - _lastBeat).TotalMilliseconds;
			return (_lastBeat = now);
		}

		/// <summary>
		/// Clears action, target, and sets state to Idle.
		/// </summary>
		private void Reset()
		{
			this.Clear();
			_state = AiState.Idle;

			if (this.Creature.IsInBattleStance)
				this.Creature.IsInBattleStance = false;

			if (this.Creature.Target != null)
			{
				this.Creature.Target = null;
				Send.SetCombatTarget(this.Creature, 0, 0);
			}
		}

		/// <summary>
		/// Changes state based on (potential) targets.
		/// </summary>
		private void SelectState()
		{
			var pos = this.Creature.GetPosition();

			// Get perceivable targets
			var radius = Math.Max(_aggroRadius, _visualRadius);
			var potentialTargets = this.Creature.Region.GetVisibleCreaturesInRange(this.Creature, radius).Where(c => !c.Warping);
			potentialTargets = potentialTargets.Where(a => this.CanPerceive(pos, this.Creature.Direction, a.GetPosition()));

			// Stay in idle if there's no visible creature in aggro range
			if (!potentialTargets.Any() && this.Creature.Target == null)
			{
				if (_state != AiState.Idle)
					this.Reset();

				return;
			}

			// Find a new target
			if (this.Creature.Target == null)
			{
				// Get hated targets
				var hated = potentialTargets.Where(cr => !cr.IsDead && this.DoesHate(cr) && !cr.Has(CreatureStates.NamedNpc));
				var hatedCount = hated.Count();

				// Get doubted targets
				var doubted = potentialTargets.Where(cr => !cr.IsDead && this.DoesDoubt(cr) && !cr.Has(CreatureStates.NamedNpc));
				var doubtedCount = doubted.Count();

				// Get loved targets
				var loved = potentialTargets.Where(cr => !cr.IsDead && this.DoesLove(cr));
				var lovedCount = loved.Count();

				// Handle hate and doubt
				if (hatedCount != 0 || doubtedCount != 0)
				{
					// Try to hate first, then doubt
					if (hatedCount != 0)
						this.Creature.Target = hated.ElementAt(this.Random(hatedCount));
					else
						this.Creature.Target = doubted.ElementAt(this.Random(doubtedCount));

					// Switch to aware
					_state = AiState.Aware;
					_awareTime = DateTime.Now;
				}
				// Handle love
				else if (lovedCount != 0)
				{
					this.Creature.Target = loved.ElementAt(this.Random(lovedCount));

					_state = AiState.Love;
				}
				// Stop if no targets were found
				else return;

				// Stop for this tick, the aware delay needs a moment anyway
				return;
			}

			// TODO: Monsters switch targets under certain circumstances,
			//   e.g. a wolf will aggro a player, even if it has already
			//   noticed a cow.

			// Reset on...
			if (this.Creature.Target.IsDead																 // target dead
			|| this.Creature.Region == Region.Limbo                                                      // invalid region (e.g. unsummoned pet)
			|| !this.Creature.GetPosition().InRange(this.Creature.Target.GetPosition(), _aggroMaxRadius) // out of aggro range
			|| this.Creature.Target.Warping																 // target is warping
			|| this.Creature.Target.Client.State == ClientState.Dead									 // target disconnected
			|| (_state != AiState.Aggro && this.Creature.Target.Conditions.Has(ConditionsA.Invisible))	 // target hid before reaching aggro state
			)
			{
				this.Reset();
				return;
			}

			// Switch to alert from aware after the delay
			if (_state == AiState.Aware && DateTime.Now >= _awareTime + _alertDelay)
			{
				// Check if target is still in immediate range
				if (this.CanPerceive(pos, this.Creature.Direction, this.Creature.Target.GetPosition()))
				{
					this.Clear();

					_state = AiState.Alert;
					_alertTime = DateTime.Now;
					this.Creature.IsInBattleStance = true;

					Send.SetCombatTarget(this.Creature, this.Creature.Target.EntityId, TargetMode.Alert);
				}
				// Reset if target ran away like a coward.
				else
				{
					this.Reset();
					return;
				}
			}

			// Switch to aggro from alert
			if (_state == AiState.Alert &&
			(
				// Aggro hated creatures after aggro delay
				(this.DoesHate(this.Creature.Target) && DateTime.Now >= _alertTime + _aggroDelay) ||

				// Aggro battle stance targets
				(_hatesBattleStance && this.Creature.Target.IsInBattleStance && DateTime.Now >= _alertTime + _hateBattleStanceDelay) ||

				// Hate over time
				(DateTime.Now >= _awareTime + _hateOverTimeDelay)
			))
			{
				// Check aggro limit
				var aggroCount = this.Creature.Region.CountAggro(this.Creature.Target, this.Creature.RaceId);
				if (aggroCount >= (int)_aggroLimit) return;

				this.Clear();

				_state = AiState.Aggro;
				Send.SetCombatTarget(this.Creature, this.Creature.Target.EntityId, TargetMode.Aggro);
			}
		}

		/// <summary>
		/// Returns true if AI can hear or see at target pos from pos.
		/// </summary>
		/// <param name="pos">Position AI's creature is at.</param>
		/// <param name="direction">AI creature's current direction.</param>
		/// <param name="targetPos">Position of the potential target.</param>
		/// <returns></returns>
		protected virtual bool CanPerceive(Position pos, byte direction, Position targetPos)
		{
			return (this.CanHear(pos, targetPos) || this.CanSee(pos, direction, targetPos));
		}

		/// <summary>
		/// Returns true if target position is within hearing range.
		/// </summary>
		/// <param name="pos">Position from which AI creature listens.</param>
		/// <param name="targetPos">Position of the potential target.</param>
		/// <returns></returns>
		protected virtual bool CanHear(Position pos, Position targetPos)
		{
			return pos.InRange(targetPos, _aggroRadius);
		}

		/// <summary>
		/// Returns true if target position is within visual field.
		/// </summary>
		/// <param name="pos">Position from which AI creature listens.</param>
		/// <param name="direction">AI creature's current direction.</param>
		/// <param name="targetPos">Position of the potential target.</param>
		/// <returns></returns>
		protected virtual bool CanSee(Position pos, byte direction, Position targetPos)
		{
			return targetPos.InCone(pos, MabiMath.ByteToRadian(direction), _visualRadius, _visualRadian);
		}

		/// <summary>
		/// Idle state
		/// </summary>
		protected virtual IEnumerable Idle()
		{
			yield break;
		}

		/// <summary>
		/// Alert state
		/// </summary>
		protected virtual IEnumerable Alert()
		{
			yield break;
		}

		/// <summary>
		/// Aggro state
		/// </summary>
		protected virtual IEnumerable Aggro()
		{
			yield break;
		}

		/// <summary>
		/// Love state
		/// </summary>
		protected virtual IEnumerable Love()
		{
			yield break;
		}

		// Setup
		// ------------------------------------------------------------------

		/// <summary>
		/// Changes the hearbeat interval.
		/// </summary>
		/// <param name="interval"></param>
		protected void SetHeartbeat(int interval)
		{
			_heartbeat = Math.Max(MinHeartbeat, interval);
			_heartbeatTimer.Change(_heartbeat, _heartbeat);
		}

		/// <summary>
		/// Milliseconds before creature notices.
		/// </summary>
		/// <param name="time"></param>
		protected void SetAlertDelay(int time)
		{
			_alertDelay = TimeSpan.FromMilliseconds(time);
		}

		/// <summary>
		/// Milliseconds before creature attacks.
		/// </summary>
		/// <param name="time"></param>
		protected void SetAggroDelay(int time)
		{
			_aggroDelay = TimeSpan.FromMilliseconds(time);
		}

		/// <summary>
		/// Radius in which creature become potential targets.
		/// </summary>
		/// <param name="radius"></param>
		protected void SetAggroRadius(int radius)
		{
			_aggroRadius = radius;
		}

		/// <summary>
		/// Sets visual field used for aggroing.
		/// </summary>
		/// <param name="radius"></param>
		/// <param name="angle"></param>
		protected void SetVisualField(int radius, double angle)
		{
			var a = Math2.Clamp(0, 160, (int)angle);

			_visualRadius = radius;
			_visualRadian = MabiMath.DegreeToRadian(a);
		}

		/// <summary>
		/// The way the AI decides whether to go into Alert/Aggro.
		/// </summary>
		/// <param name="type"></param>
		protected void SetAggroType(AggroType type)
		{
			//_aggroType = type;
			Log.Warning("{0}: SetAggroType is obsolete, use 'Doubts' and 'HatesBattleStance' instead.", this.GetType().Name);
		}

		/// <summary>
		/// Milliseconds before creature attacks.
		/// </summary>
		/// <param name="limit"></param>
		protected void SetAggroLimit(AggroLimit limit)
		{
			_aggroLimit = limit;
		}

		/// <summary>
		/// Adds a race tag that the AI hates and will target.
		/// </summary>
		/// <param name="tags"></param>
		protected void Hates(params string[] tags)
		{
			foreach (var tag in tags)
			{
				var key = tag.Trim(' ', '/');
				if (_hateTags.ContainsKey(key))
					return;

				_hateTags.Add(key, tag);
			}
		}

		/// <summary>
		/// Adds a race tag that the AI likes and will not target unless
		/// provoked.
		/// </summary>
		/// <param name="tags"></param>
		protected void Loves(params string[] tags)
		{
			foreach (var tag in tags)
			{
				var key = tag.Trim(' ', '/');
				if (_loveTags.ContainsKey(key))
					return;

				_loveTags.Add(key, tag);
			}
		}

		/// <summary>
		/// Adds a race tag that the AI doubts.
		/// </summary>
		/// <param name="tags"></param>
		protected void Doubts(params string[] tags)
		{
			foreach (var tag in tags)
			{
				var key = tag.Trim(' ', '/');
				if (_hateTags.ContainsKey(key))
					return;

				_doubtTags.Add(key, tag);
			}
		}

		/// <summary>
		/// Specifies that the AI will go from alert into aggro when enemy
		/// changes into battle mode.
		/// </summary>
		protected void HatesBattleStance(int delay = 3000)
		{
			_hatesBattleStance = true;
			_hateBattleStanceDelay = TimeSpan.FromMilliseconds(delay);
		}

		/// <summary>
		/// Specifies that the AI will go from alert into aggro when a
		/// doubted target sticks around for too long.
		/// </summary>
		/// <param name="delay"></param>
		protected void HatesNearby(int delay = 6000)
		{
			_hateOverTimeDelay = TimeSpan.FromMilliseconds(delay);
		}

		/// <summary>
		/// Sets the max distance an NPC can wander away from its spawn.
		/// </summary>
		/// <param name="distance"></param>
		protected void SetMaxDistanceFromSpawn(int distance)
		{
			_maxDistanceFromSpawn = distance;
		}

		/// <summary>
		/// Reigsters a reaction.
		/// </summary>
		/// <param name="ev">The event on which func should be executed.</param>
		/// <param name="func">The reaction to the event.</param>
		protected void On(AiState state, AiEvent ev, Func<IEnumerable> func)
		{
			this.On(state, ev, SkillId.None, func);
		}

		/// <summary>
		/// Reigsters a reaction.
		/// </summary>
		/// <param name="state">The state the event is for.</param>
		/// <param name="ev">The event on which func should be executed.</param>
		/// <param name="skillId">The skill the should trigger the event.</param>
		/// <param name="func">The reaction to the event.</param>
		protected void On(AiState state, AiEvent ev, SkillId skillId, Func<IEnumerable> func)
		{
			lock (_reactions)
			{
				if (!_reactions[state].ContainsKey(ev))
					_reactions[state][ev] = new Dictionary<SkillId, Func<IEnumerable>>();
				_reactions[state][ev][skillId] = func;
			}
		}

		// Functions
		// ------------------------------------------------------------------

		/// <summary>
		/// Returns random number between 0.0 and 100.0.
		/// </summary>
		/// <returns></returns>
		protected double Random()
		{
			lock (_rnd)
				return (100 * _rnd.NextDouble());
		}

		/// <summary>
		/// Returns random number between 0 and max-1.
		/// </summary>
		/// <param name="max">Exclusive upper bound</param>
		/// <returns></returns>
		protected int Random(int max)
		{
			lock (_rnd)
				return _rnd.Next(max);
		}

		/// <summary>
		/// Returns random number between min and max-1.
		/// </summary>
		/// <param name="min">Inclusive lower bound</param>
		/// <param name="max">Exclusive upper bound</param>
		/// <returns></returns>
		protected int Random(int min, int max)
		{
			lock (_rnd)
				return _rnd.Next(min, max);
		}

		/// <summary>
		/// Returns a random value from the given ones.
		/// </summary>
		/// <param name="values"></param>
		protected T Rnd<T>(params T[] values)
		{
			lock (_rnd)
				return _rnd.Rnd(values);
		}

		/// <summary>
		/// Returns true if AI hates target creature.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		protected bool DoesHate(Creature target)
		{
			return _hateTags.Values.Any(tag => target.RaceData.HasTag(tag));
		}

		/// <summary>
		/// Returns true if AI loves target creature.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		protected bool DoesLove(Creature target)
		{
			return _loveTags.Values.Any(tag => target.RaceData.HasTag(tag));
		}

		/// <summary>
		/// Returns true if AI doubts target creature.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		protected bool DoesDoubt(Creature target)
		{
			return _doubtTags.Values.Any(tag => target.RaceData.HasTag(tag));
		}

		/// <summary>
		/// Sends SharpMind to all applicable creatures.
		/// </summary>
		/// <remarks>
		/// The Wiki is speaking of a passive Sharp Mind skill, but it doesn't
		/// seem to be a skill at all anymore.
		/// 
		/// A failed Sharp Mind is supposed to be displayed as an "X",
		/// assumingly statuses 3 and 4 were used for this in the past,
		/// but the current NA client doesn't do anything when sending
		/// them, so we use skill id 0 instead, which results in a
		/// question mark, originally used for skills unknown to the
		/// player.
		/// 
		/// Even on servers that didn't have Sharp Mind officially,
		/// the packets were still sent to the client, it just didn't
		/// display them, assumingly because the players didn't have
		/// the skill. Since this is not the case for the NA client,
		/// we control it from the server.
		/// 
		/// TODO: When we move AIs to an NPC client, the entire SharpMind
		///   handling would move to the SkillPrepare handler.
		/// </remarks>
		/// <param name="skillId"></param>
		/// <param name="status"></param>
		protected void SharpMind(SkillId skillId, SharpMindStatus status)
		{
			// Some races are "immune" to Sharp Mind
			if (this.Creature.RaceData.SharpMindImmune)
				return;

			// Check if SharpMind is enabled
			if (!AuraData.FeaturesDb.IsEnabled("SharpMind"))
				return;

			var passive = AuraData.FeaturesDb.IsEnabled("PassiveSharpMind");

			// Send to players in range, one after the other, so we have control
			// over the recipients.
			foreach (var creature in _playersInRange)
			{
				// Handle active (old) Sharp Mind
				if (!passive)
				{
					// Don't send if player doesn't have Sharp Mind.
					if (!creature.Skills.Has(SkillId.SharpMind))
						continue;

					var success = (this.Random() < ChannelServer.Instance.Conf.World.SharpMindChance);

					// Set skill id to 0, so the bubble displays a question mark,
					// if skill is unknown to the player or Sharp Mind fails.
					if (!creature.Skills.Has(skillId) || !success)
						skillId = SkillId.None;

					SharpMindHandler.Train(this.Creature, creature, success);
				}

				// Cancel and None are sent for removing the bubble
				if (status == SharpMindStatus.Cancelling || status == SharpMindStatus.None)
				{
					Send.SharpMind(this.Creature, creature, skillId, SharpMindStatus.Cancelling);
					Send.SharpMind(this.Creature, creature, skillId, SharpMindStatus.None);
				}
				else
				{
					Send.SharpMind(this.Creature, creature, skillId, status);
				}
			}
		}

		/// <summary>
		/// Proxy for Localization.Get.
		/// </summary>
		/// <param name="phrase"></param>
		protected static string L(string phrase)
		{
			return Localization.Get(phrase);
		}

		/// <summary>
		/// Proxy for Localization.GetParticular.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="phrase"></param>
		protected static string LX(string context, string phrase)
		{
			return Localization.GetParticular(context, phrase);
		}

		/// <summary>
		/// Proxy for Localization.GetPlural.
		/// </summary>
		/// <param name="phrase"></param>
		/// <param name="phrasePlural"></param>
		/// <param name="count"></param>
		protected static string LN(string phrase, string phrasePlural, int count)
		{
			return Localization.GetPlural(phrase, phrasePlural, count);
		}

		/// <summary>
		/// Proxy for Localization.GetParticularPlural.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="phrase"></param>
		/// <param name="phrasePlural"></param>
		/// <param name="count"></param>
		protected static string LXN(string context, string phrase, string phrasePlural, int count)
		{
			return Localization.GetParticularPlural(context, phrase, phrasePlural, count);
		}

		/// <summary>
		/// Returns true if AI creature has the skill.
		/// </summary>
		/// <param name="skillId"></param>
		/// <returns></returns>
		protected bool HasSkill(SkillId skillId)
		{
			return this.Creature.Skills.Has(skillId);
		}

		/// <summary>
		/// Generates and saves a random number between 0 and 99,
		/// for Case to use.
		/// </summary>
		/// <remarks>
		/// SwitchRandom only keeps track of one random number at a time.
		/// You can nest SwitchRandom-if-constructs, but randomly calling
		/// SwitchRandom in between might give unexpected results.
		/// </remarks>
		/// <example>
		/// SwitchRandom();
		/// if (Case(40))
		/// {
		///     Do(Wander(250, 500));
		/// }
		/// else if (Case(40))
		/// {
		///     Do(Wander(250, 500, false));
		/// }
		/// else if (Case(20))
		/// {
		///     Do(Wait(4000, 6000));
		/// }
		/// 
		/// SwitchRandom();
		/// if (Case(60))
		/// {
		///		SwitchRandom();
		///		if (Case(20))
		///		{
		///		    Do(Wander(250, 500));
		///		}
		///		else if (Case(80))
		///		{
		///		    Do(Wait(4000, 6000));
		///		}
		/// }
		/// else if (Case(40))
		/// {
		///     Do(Wander(250, 500, false));
		/// }
		/// </example>
		protected void SwitchRandom()
		{
			_switchRandomN = this.Random(100);
			_switchRandomM = 0;
		}

		/// <summary>
		/// Returns true if value matches the last random percentage
		/// generated by SwitchRandom().
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected bool Case(int value)
		{
			_switchRandomM += value;
			return (_switchRandomN < _switchRandomM);
		}

		// Flow control
		// ------------------------------------------------------------------

		/// <summary>
		/// Cleares action queue.
		/// </summary>
		protected void Clear()
		{
			_curAction = null;
		}

		/// <summary>
		/// Clears AI and sets new current action.
		/// </summary>
		/// <param name="action"></param>
		protected void SwitchAction(Func<IEnumerable> action)
		{
			this.ExecuteOnce(this.CancelSkill());

			// Cancel rest
			if (this.Creature.Has(CreatureStates.SitDown))
			{
				var restHandler = ChannelServer.Instance.SkillManager.GetHandler<Rest>(SkillId.Rest);
				if (restHandler != null)
					restHandler.Stop(this.Creature, this.Creature.Skills.Get(SkillId.Rest));
			}

			_curAction = action().GetEnumerator();
		}

		/// <summary>
		/// Creates enumerator and runs it once.
		/// </summary>
		/// <remarks>
		/// Useful if you want to make a creature go somewhere, but you don't
		/// want to wait for it to arrive there. Effectively running the action
		/// with a 0 timeout.
		/// </remarks>
		/// <param name="action"></param>
		protected void ExecuteOnce(IEnumerable action)
		{
			action.GetEnumerator().MoveNext();
		}

		/// <summary>
		/// Sets target and puts creature in battle mode.
		/// </summary>
		/// <param name="creature"></param>
		public void AggroCreature(Creature creature)
		{
			_state = AiState.Aggro;
			this.Clear();
			this.Creature.IsInBattleStance = true;
			this.Creature.Target = creature;
			Send.SetCombatTarget(this.Creature, this.Creature.Target.EntityId, TargetMode.Aggro);
		}

		// Actions
		// ------------------------------------------------------------------

		/// <summary>
		/// Makes creature say something in public chat.
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		protected IEnumerable Say(string msg)
		{
			if (!string.IsNullOrWhiteSpace(msg))
				Send.Chat(this.Creature, msg);

			yield break;
		}

		/// <summary>
		/// Makes creature say one of the messages in public chat.
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		protected IEnumerable Say(params string[] msgs)
		{
			if (msgs == null || msgs.Length == 0)
				yield break;

			var msg = msgs[this.Random(msgs.Length)];
			if (!string.IsNullOrWhiteSpace(msg))
				Send.Chat(this.Creature, msg);

			yield break;
		}

		/// <summary>
		/// Makes creature say a random phrase in public chat.
		/// </summary>
		/// <returns></returns>
		protected IEnumerable SayRandomPhrase()
		{
			if (this.Phrases.Count > 0)
				Send.Chat(this.Creature, this.Phrases[this.Random(this.Phrases.Count)]);
			yield break;
		}

		/// <summary>
		/// Makes AI wait for a random amount of ms, between min and max.
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		protected IEnumerable Wait(int min, int max = 0)
		{
			if (max < min)
				max = min;

			var duration = (min == max ? min : this.Random(min, max + 1));
			var target = _timestamp + duration;

			while (_timestamp < target)
			{
				yield return true;
			}
		}

		/// <summary>
		/// Makes creature walk to a random position in range.
		/// </summary>
		/// <param name="minDistance"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		protected IEnumerable Wander(int minDistance = 100, int maxDistance = 600, bool walk = true)
		{
			if (maxDistance < minDistance)
				maxDistance = minDistance;

			var rnd = RandomProvider.Get();
			var pos = this.Creature.GetPosition();
			var destination = pos.GetRandomInRange(minDistance, maxDistance, rnd);

			// Make sure NPCs don't wander off
			var npc = this.Creature as NPC;
			if (npc != null && destination.GetDistance(npc.SpawnLocation.Position) > _maxDistanceFromSpawn)
				destination = pos.GetRelative(npc.SpawnLocation.Position, (minDistance + maxDistance) / 2);

			foreach (var action in this.MoveTo(destination, walk))
				yield return action;
		}

		/// <summary>
		/// Runs action till it's done or the timeout is reached.
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		protected IEnumerable Timeout(double timeout, IEnumerable action)
		{
			timeout += _timestamp;

			foreach (var a in action)
			{
				if (_timestamp >= timeout)
					yield break;
				yield return true;
			}
		}

		/// <summary>
		/// Creature runs to destination.
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		protected IEnumerable RunTo(Position destination)
		{
			return this.MoveTo(destination, false);
		}

		/// <summary>
		/// Creature walks to destination.
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		protected IEnumerable WalkTo(Position destination)
		{
			return this.MoveTo(destination, true);
		}

		/// <summary>
		/// Creature moves to destination.
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="walk"></param>
		/// <returns></returns>
		protected IEnumerable MoveTo(Position destination, bool walk)
		{
			var pos = this.Creature.GetPosition();

			// Limit AI to walking if Defense is active (hot-fix)
			if (this.Creature.Skills.IsActive(SkillId.Defense))
				walk = true;

			// Check for collision
			Position intersection;
			if (this.Creature.Region.Collisions.Find(pos, destination, out intersection))
			{
				destination = pos.GetRelative(intersection, -100);

				// If new destination is invalid as well don't move at all
				if (this.Creature.Region.Collisions.Any(pos, destination))
					destination = pos;
			}

			this.Creature.Move(destination, walk);

			var time = this.Creature.MoveDuration * 1000;
			var walkTime = _timestamp + time;

			do
			{
				// Yield at least once, even if it took 0 time,
				// to avoid unexpected problems, like infinite outer loops,
				// because an action expected the walk to yield at least once.
				yield return true;
			}
			while (_timestamp < walkTime);
		}

		/// <summary>
		/// Creature circles around target.
		/// </summary>
		/// <param name="radius"></param>
		/// <param name="timeMin"></param>
		/// <param name="timeMax"></param>
		/// <returns></returns>
		protected IEnumerable Circle(int radius, int timeMin = 1000, int timeMax = 5000, bool walk = true)
		{
			return this.Circle(radius, timeMin, timeMax, this.Random() < 50, walk);
		}

		/// <summary>
		/// Creature circles around target.
		/// </summary>
		/// <param name="radius"></param>
		/// <param name="timeMin"></param>
		/// <param name="timeMax"></param>
		/// <param name="clockwise"></param>
		/// <returns></returns>
		protected IEnumerable Circle(int radius, int timeMin, int timeMax, bool clockwise, bool walk)
		{
			if (timeMin < 500)
				timeMin = 500;
			if (timeMax < timeMin)
				timeMax = timeMin;

			var time = (timeMin == timeMax ? timeMin : this.Random(timeMin, timeMax + 1));
			var until = _timestamp + time;

			for (int i = 0; _timestamp < until || i == 0; ++i)
			{
				// Stop if target vanished somehow
				if (this.Creature.Target == null)
					yield break;

				var targetPos = this.Creature.Target.GetPosition();
				var pos = this.Creature.GetPosition();

				var deltaX = pos.X - targetPos.X;
				var deltaY = pos.Y - targetPos.Y;
				var angle = Math.Atan2(deltaY, deltaX) + (Math.PI / 8 * 2) * (clockwise ? -1 : 1);

				var x = targetPos.X + (Math.Cos(angle) * radius);
				var y = targetPos.Y + (Math.Sin(angle) * radius);
				var actualMovePos = new Position((int)x, (int)y);

				// Move a little further, so the creature doesn't stop for
				// a tick while calculating the next position to walk to
				var distanceMovePos = pos.GetRelative(actualMovePos, radius);

				// Get time it takes to get to the actual position we want to
				// go to, so we can wait till we're there before issuing the
				// next WalkTo.
				var diffX = actualMovePos.X - pos.X;
				var diffY = actualMovePos.Y - pos.Y;
				var moveDuration = (int)(Math.Sqrt(diffX * diffX + diffY * diffY) / this.Creature.GetSpeed() * 1000);

				this.ExecuteOnce(this.MoveTo(distanceMovePos, walk));

				foreach (var action in this.Wait(moveDuration))
					yield return action;
			}

			// Stop movement after circling is done, so the creature doesn't
			// finish walking to the more distant position.
			this.Creature.StopMove();
		}

		/// <summary>
		/// Creature follows its target.
		/// </summary>
		/// <param name="maxDistance"></param>
		/// <param name="walk"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		protected IEnumerable Follow(int maxDistance, bool walk = false, int timeout = 5000)
		{
			var until = _timestamp + Math.Max(0, timeout);

			while (_timestamp < until)
			{
				// Stop if target vanished somehow
				if (this.Creature.Target == null)
					yield break;

				var pos = this.Creature.GetPosition();
				var targetPos = this.Creature.Target.GetPosition();

				if (!pos.InRange(targetPos, maxDistance))
				{
					// Walk up to distance-50 (a buffer so it really walks into range)
					this.ExecuteOnce(this.MoveTo(pos.GetRelative(targetPos, -maxDistance + 50), walk));
				}

				yield return true;
			}
		}

		/// <summary>
		/// Creature tries to get away from target.
		/// </summary>
		/// <param name="minDistance"></param>
		/// <param name="walk"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		protected IEnumerable KeepDistance(int minDistance, bool walk = false, int timeout = 5000)
		{
			var until = _timestamp + Math.Max(0, timeout);

			while (_timestamp < until)
			{
				var pos = this.Creature.GetPosition();
				var targetPos = this.Creature.Target.GetPosition();

				if (pos.InRange(targetPos, minDistance))
				{
					// The position to move to is on the line between pos and targetPos,
					// -distance from target to creature, resulting in a position
					// "behind" the creature.
					this.ExecuteOnce(this.MoveTo(pos.GetRelative(targetPos, -(minDistance + 50)), walk));
				}

				yield return true;
			}
		}

		/// <summary>
		/// Attacks target creature "KnockCount" times.
		/// </summary>
		/// <returns></returns>
		protected IEnumerable Attack()
		{
			var count = 1 + (this.Creature.Inventory.RightHand != null ? this.Creature.Inventory.RightHand.Info.KnockCount : this.Creature.RaceData.KnockCount);
			return this.Attack(count);
		}

		/// <summary>
		/// Attacks target creature x times.
		/// </summary>
		/// <returns></returns>
		protected IEnumerable Attack(int count, int timeout = 300000)
		{
			if (this.Creature.Target == null)
			{
				this.Reset();
				yield break;
			}

			timeout = Math2.Clamp(0, 300000, timeout);
			var until = _timestamp + timeout;

			// Each successful hit counts, attack until count or timeout is reached.
			for (int i = 0; ; )
			{
				// Get skill
				var skill = this.Creature.Skills.ActiveSkill;
				if (skill == null && (skill = this.Creature.Skills.Get(SkillId.CombatMastery)) == null)
				{
					Log.Warning("AI.Attack: Creature '{0}' doesn't have Combat Mastery.", this.Creature.RaceId);
					yield break;
				}

				// Get skill handler
				var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<ICombatSkill>(skill.Info.Id);
				if (skillHandler == null)
				{
					Log.Error("AI.Attack: Skill handler not found for '{0}'.", skill.Info.Id);
					yield break;
				}

				// Stop timeout was reached
				if (_timestamp >= until)
					break;

				// Stop if target vanished somehow
				if (this.Creature.Target == null)
					yield break;

				// Attack
				var result = skillHandler.Use(this.Creature, skill, this.Creature.Target.EntityId);
				if (result == CombatSkillResult.Okay)
				{
					// Stop when max attack count is reached
					if (++i >= count)
						break;

					yield return true;
				}
				else if (result == CombatSkillResult.OutOfRange)
				{
					// Run to target if out of range
					var pos = this.Creature.GetPosition();
					var targetPos = this.Creature.Target.GetPosition();

					//var attackRange = this.Creature.AttackRangeFor(this.Creature.Target);
					//this.ExecuteOnce(this.RunTo(pos.GetRelative(targetPos, -attackRange + 50)));
					this.ExecuteOnce(this.RunTo(targetPos));

					yield return true;
				}
				else if (result == CombatSkillResult.InvalidTarget)
				{
					// Reset if target couldn't be found.
					this.Reset();
					yield break;
				}
				else
				{
					Log.Error("AI.Attack: Unhandled combat skill result ({0}).", result);
					yield break;
				}
			}

			// Complete is called automatically from OnUsedSkill
		}

		/// <summary>
		/// Attacks target with a ranged attack.
		/// </summary>
		/// <param name="timeout"></param>
		/// <returns></returns>
		protected IEnumerable RangedAttack(int timeout = 5000)
		{
			var target = this.Creature.Target;

			// Check active skill
			var activeSkill = this.Creature.Skills.ActiveSkill;
			if (activeSkill != null)
			{
				if (activeSkill.Data.Type != SkillType.RangedCombat)
				{
					Log.Warning("AI.RangedAttack: Active skill is no ranged skill.", this.Creature.RaceId);
					yield break;
				}
			}
			else
			{
				// Get skill
				activeSkill = this.Creature.Skills.Get(SkillId.RangedAttack);
				if (activeSkill == null)
				{
					Log.Warning("AI.RangedAttack: Creature '{0}' doesn't have RangedAttack.", this.Creature.RaceId);
					yield break;
				}

				// Get handler
				var rangedHandler = ChannelServer.Instance.SkillManager.GetHandler<RangedAttack>(activeSkill.Info.Id);

				// Start loading
				this.SharpMind(activeSkill.Info.Id, SharpMindStatus.Loading);

				// Prepare skill
				rangedHandler.Prepare(this.Creature, activeSkill, null);

				this.Creature.Skills.ActiveSkill = activeSkill;
				activeSkill.State = SkillState.Prepared;

				// Wait for loading to be done
				foreach (var action in this.Wait(activeSkill.RankData.LoadTime))
					yield return action;

				// Call ready
				rangedHandler.Ready(this.Creature, activeSkill, null);
				activeSkill.State = SkillState.Ready;

				// Done loading
				this.SharpMind(activeSkill.Info.Id, SharpMindStatus.Loaded);
			}

			// Get combat handler for active skill
			var combatHandler = ChannelServer.Instance.SkillManager.GetHandler<ICombatSkill>(activeSkill.Info.Id);

			// Start aiming
			this.Creature.AimMeter.Start(target.EntityId);

			// Wait till aim is 99% or timeout is reached
			var until = _timestamp + Math.Max(0, timeout);
			var aim = 0.0;
			while (_timestamp < until && (aim = this.Creature.AimMeter.GetAimChance(target)) < 90)
				yield return true;

			// Cancel if 90 aim weren't reached
			if (aim < 90)
			{
				this.SharpMind(activeSkill.Info.Id, SharpMindStatus.Cancelling);
				this.Creature.Skills.CancelActiveSkill();
				this.Creature.AimMeter.Stop();
				yield break;
			}

			// Attack
			combatHandler.Use(this.Creature, activeSkill, target.EntityId);
			activeSkill.State = SkillState.Completed;

			// Complete is called automatically from OnUsedSkill
		}

		/// <summary>
		/// Attacks with the given skill, charging it first, if it doesn't
		/// have the given amount of stacks yet. Attacks until all stacks
		/// have been used, or timeout is reached.
		/// </summary>
		/// <param name="skillId"></param>
		/// <param name="stacks"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		protected IEnumerable StackAttack(SkillId skillId, int stacks = 1, int timeout = 30000)
		{
			var target = this.Creature.Target;
			var until = _timestamp + Math.Max(0, timeout);

			// Get handler
			var prepareHandler = ChannelServer.Instance.SkillManager.GetHandler<IPreparable>(skillId);
			var readyHandler = prepareHandler as IReadyable;
			var combatHandler = prepareHandler as ICombatSkill;

			if (prepareHandler == null || readyHandler == null || combatHandler == null)
			{
				Log.Warning("AI.StackAttack: {0}'s handler doesn't exist, or doesn't implement the necessary interfaces.", skillId);
				yield break;
			}

			// Cancel active skill if it's not the one we want
			var skill = this.Creature.Skills.ActiveSkill;
			if (skill != null && skill.Info.Id != skillId)
			{
				foreach (var action in this.CancelSkill())
					yield return action;
			}

			// Get skill if we don't have one yet
			if (skill == null)
			{
				// Get skill
				skill = this.Creature.Skills.Get(skillId);
				if (skill == null)
				{
					Log.Warning("AI.StackAttack: Creature '{0}' doesn't have {1}.", this.Creature.RaceId, skillId);
					yield break;
				}
			}

			// Stack up
			stacks = Math2.Clamp(1, skill.RankData.StackMax, stacks);
			while (skill.Stacks < stacks)
			{
				// Start loading
				this.SharpMind(skill.Info.Id, SharpMindStatus.Loading);

				// Prepare skill
				prepareHandler.Prepare(this.Creature, skill, null);

				this.Creature.Skills.ActiveSkill = skill;
				skill.State = SkillState.Prepared;

				// Wait for loading to be done
				foreach (var action in this.Wait(skill.RankData.LoadTime))
					yield return action;

				// Call ready
				readyHandler.Ready(this.Creature, skill, null);
				skill.State = SkillState.Ready;

				// Done loading
				this.SharpMind(skill.Info.Id, SharpMindStatus.Loaded);
			}

			// Small delay
			foreach (var action in this.Wait(1000, 2000))
				yield return action;

			// Attack
			while (skill.Stacks > 0)
			{
				if (_timestamp >= until)
					break;

				combatHandler.Use(this.Creature, skill, target.EntityId);
				yield return true;
			}

			// Cancel skill if there are left over stacks
			if (skill.Stacks != 0)
			{
				foreach (var action in this.CancelSkill())
					yield return action;
			}
		}

		/// <summary>
		/// Makes creature prepare given skill.
		/// </summary>
		/// <param name="skillId"></param>
		/// <returns></returns>
		protected IEnumerable PrepareSkill(SkillId skillId)
		{
			return this.PrepareSkill(skillId, 1);
		}

		/// <summary>
		/// Makes creature prepare given skill.
		/// </summary>
		/// <param name="skillId"></param>
		/// <returns></returns>
		protected IEnumerable PrepareSkill(SkillId skillId, int stacks)
		{
			// Get skill
			var skill = this.Creature.Skills.Get(skillId);
			if (skill == null)
			{
				// The AIs are designed to work with multiple races,
				// even if they might not possess certain skills.
				// We don't need a warning if they don't have the skill,
				// they simply shouldn't do anything in that case.

				//Log.Warning("AI.PrepareSkill: AI '{0}' tried to prepare skill '{2}', that its creature '{1}' doesn't have.", this.GetType().Name, this.Creature.RaceId, skillId);
				yield break;
			}

			// Cancel previous skill
			var activeSkill = this.Creature.Skills.ActiveSkill;
			if (activeSkill != null && activeSkill.Info.Id != skillId)
				this.ExecuteOnce(this.CancelSkill());

			stacks = Math2.Clamp(1, skill.RankData.StackMax, skill.Stacks + stacks);
			while (skill.Stacks < stacks)
			{
				// Explicit handling
				if (skillId == SkillId.WebSpinning)
				{
					var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<WebSpinning>(skillId);
					skillHandler.Prepare(this.Creature, skill, null);
					this.Creature.Skills.ActiveSkill = skill;
					skillHandler.Complete(this.Creature, skill, null);
					this.Creature.Skills.ActiveSkill = null;
				}
				else if (skillId == SkillId.DarkLord)
				{
					var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<DarkLordSkill>(skillId);
					skillHandler.Prepare(this.Creature, skill, null);
					this.Creature.Skills.ActiveSkill = skill;
					skillHandler.Complete(this.Creature, skill, null);
					this.Creature.Skills.ActiveSkill = null;
				}
				// Try to handle implicitly
				else
				{
					// Get preparable handler
					var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<IPreparable>(skillId);
					if (skillHandler == null)
					{
						Log.Unimplemented("AI.PrepareSkill: Missing handler or IPreparable for '{0}'.", skillId);
						yield break;
					}

					// Get readyable handler.
					// TODO: There are skills that don't have ready, but go right to
					//   use from Prepare. Handle somehow.
					var readyHandler = skillHandler as IReadyable;
					if (readyHandler == null)
					{
						Log.Unimplemented("AI.PrepareSkill: Missing IReadyable for '{0}'.", skillId);
						yield break;
					}

					this.SharpMind(skillId, SharpMindStatus.Loading);

					// Prepare skill
					try
					{
						if (!skillHandler.Prepare(this.Creature, skill, null))
							yield break;

						this.Creature.Skills.ActiveSkill = skill;
						skill.State = SkillState.Prepared;
					}
					catch (NullReferenceException)
					{
						Log.Warning("AI.PrepareSkill: Null ref exception while preparing '{0}', skill might have parameters.", skillId);
					}
					catch (NotImplementedException)
					{
						Log.Unimplemented("AI.PrepareSkill: Skill prepare method for '{0}'.", skillId);
					}

					// Wait for loading to be done
					foreach (var action in this.Wait(skill.RankData.LoadTime))
						yield return action;

					// Call ready
					readyHandler.Ready(this.Creature, skill, null);
					skill.State = SkillState.Ready;

					this.SharpMind(skillId, SharpMindStatus.Loaded);
				}

				// If stacks are still 0 after preparing, we'll have to assume
				// that the skill didn't set it. We have to break the loop,
				// otherwise the AI would prepare the skill indefinitely.
				if (skill.Stacks == 0)
					break;
			}
		}

		/// <summary>
		/// Makes creature cancel currently loaded skill.
		/// </summary>
		/// <returns></returns>
		protected IEnumerable CancelSkill()
		{
			if (this.Creature.Skills.ActiveSkill != null)
			{
				this.SharpMind(this.Creature.Skills.ActiveSkill.Info.Id, SharpMindStatus.Cancelling);
				this.Creature.Skills.CancelActiveSkill();
			}

			yield break;
		}

		/// <summary>
		/// Makes creature use currently loaded skill.
		/// </summary>
		/// <returns></returns>
		protected IEnumerable UseSkill()
		{
			var activeSkillId = this.Creature.Skills.ActiveSkill != null ? this.Creature.Skills.ActiveSkill.Info.Id : SkillId.None;

			if (activeSkillId == SkillId.None)
				yield break;

			if (activeSkillId == SkillId.Windmill)
			{
				var wmHandler = ChannelServer.Instance.SkillManager.GetHandler<Windmill>(activeSkillId);
				wmHandler.Use(this.Creature, this.Creature.Skills.ActiveSkill, 0, 0, 0);
				this.SharpMind(activeSkillId, SharpMindStatus.Cancelling);
			}
			else if (activeSkillId == SkillId.Stomp)
			{
				var handler = ChannelServer.Instance.SkillManager.GetHandler<Stomp>(activeSkillId);
				handler.Use(this.Creature, this.Creature.Skills.ActiveSkill, 0, 0, 0);
				this.SharpMind(activeSkillId, SharpMindStatus.Cancelling);
			}
			else if (activeSkillId == SkillId.GlasGhaibhleannSkill)
			{
				var pos = this.Creature.GetPosition();
				var targetPos = this.Creature.Target.GetPosition();
				var hitPos = targetPos.GetRelative(pos, -1000);
				var hitLocation = new Location(this.Creature.RegionId, hitPos);
				var targetAreaEntityId = hitLocation.ToLocationId();

				var handler = ChannelServer.Instance.SkillManager.GetHandler<GlasGhaibhleannSkill>(activeSkillId);
				handler.Use(this.Creature, this.Creature.Skills.ActiveSkill, targetAreaEntityId);
				this.SharpMind(activeSkillId, SharpMindStatus.Cancelling);
			}
			else if (activeSkillId == SkillId.Fireball)
			{
				var target = this.Creature.Target;

				var handler = ChannelServer.Instance.SkillManager.GetHandler<Fireball>(activeSkillId);
				handler.Use(this.Creature, this.Creature.Skills.ActiveSkill, target.EntityId, 0, 0);
				this.SharpMind(activeSkillId, SharpMindStatus.Cancelling);
			}
			else
			{
				Log.Unimplemented("AI.UseSkill: Skill '{0}'", activeSkillId);
			}
		}

		/// <summary>
		/// Makes creature cancel currently loaded skill.
		/// </summary>
		/// <returns></returns>
		protected IEnumerable CompleteSkill()
		{
			if (this.Creature.Skills.ActiveSkill == null)
				yield break;

			var skill = this.Creature.Skills.ActiveSkill;
			var skillId = this.Creature.Skills.ActiveSkill.Info.Id;

			// Get skill handler
			var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<ICompletable>(skillId);
			if (skillHandler == null)
			{
				Log.Unimplemented("AI.CompleteSkill: Missing handler or ICompletable for '{0}'.", skillId);
				yield break;
			}

			// Run complete
			try
			{
				skillHandler.Complete(this.Creature, skill, null);
			}
			catch (NullReferenceException)
			{
				Log.Warning("AI.CompleteSkill: Null ref exception while preparing '{0}', skill might have parameters.", skillId);
			}
			catch (NotImplementedException)
			{
				Log.Unimplemented("AI.CompleteSkill: Skill complete method for '{0}'.", skillId);
			}

			// Finalize complete or ready again
			if (skill.Stacks == 0)
			{
				this.Creature.Skills.ActiveSkill = null;
				skill.State = SkillState.Completed;
				this.SharpMind(skillId, SharpMindStatus.Cancelling);
			}
			else if (skill.State != SkillState.Canceled)
			{
				skill.State = SkillState.Ready;
			}
		}

		/// <summary>
		/// Makes creature start given skill.
		/// </summary>
		/// <param name="skillId"></param>
		/// <returns></returns>
		protected IEnumerable StartSkill(SkillId skillId)
		{
			// Get skill
			var skill = this.Creature.Skills.Get(skillId);
			if (skill == null)
			{
				Log.Warning("AI.StartSkill: AI '{0}' tried to start skill '{2}', that its creature '{1}' doesn't have.", this.GetType().Name, this.Creature.RaceId, skillId);
				yield break;
			}

			// Get handler
			var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<IStartable>(skillId);
			if (skillHandler == null)
			{
				Log.Unimplemented("AI.StartSkill: Missing handler or interface for '{0}'.", skillId);
				yield break;
			}

			// Run handler
			try
			{
				if (skillHandler is Rest)
				{
					var restHandler = (Rest)skillHandler;
					restHandler.Start(this.Creature, skill, MabiDictionary.Empty);
				}
				else
				{
					skillHandler.Start(this.Creature, skill, null);
				}
			}
			catch (NullReferenceException)
			{
				Log.Warning("AI.StartSkill: Null ref exception while starting '{0}', skill might have parameters.", skillId);
			}
			catch (NotImplementedException)
			{
				Log.Unimplemented("AI.StartSkill: Skill start method for '{0}'.", skillId);
			}
		}

		/// <summary>
		/// Makes creature stop given skill.
		/// </summary>
		/// <param name="skillId"></param>
		/// <returns></returns>
		protected IEnumerable StopSkill(SkillId skillId)
		{
			// Get skill
			var skill = this.Creature.Skills.Get(skillId);
			if (skill == null)
			{
				Log.Warning("AI.StopSkill: AI '{0}' tried to stop skill '{2}', that its creature '{1}' doesn't have.", this.GetType().Name, this.Creature.RaceId, skillId);
				yield break;
			}

			// Get handler
			var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<IStoppable>(skillId);
			if (skillHandler == null)
			{
				Log.Unimplemented("AI.StopSkill: Missing handler or interface for '{0}'.", skillId);
				yield break;
			}

			// Run handler
			try
			{
				if (skillHandler is Rest)
				{
					var restHandler = (Rest)skillHandler;
					restHandler.Stop(this.Creature, skill, MabiDictionary.Empty);
				}
				else
				{
					skillHandler.Stop(this.Creature, skill, null);
				}
			}
			catch (NullReferenceException)
			{
				Log.Warning("AI.StopSkill: Null ref exception while stopping '{0}', skill might have parameters.", skillId);
			}
			catch (NotImplementedException)
			{
				Log.Unimplemented("AI.StopSkill: Skill stop method for '{0}'.", skillId);
			}
		}

		/// <summary>
		/// Switches to the given weapon set.
		/// </summary>
		/// <param name="set"></param>
		/// <returns></returns>
		protected IEnumerable SwitchTo(WeaponSet set)
		{
			if (this.Creature.Inventory.WeaponSet == set)
				yield break;

			// Wait a moment before and after switching,
			// to let the animation play.
			var waitTime = 500;

			foreach (var action in this.Wait(waitTime))
				yield return action;

			this.Creature.Inventory.ChangeWeaponSet(set);

			foreach (var action in this.Wait(waitTime))
				yield return action;
		}

		/// <summary>
		/// Changes the AI's creature's height.
		/// </summary>
		/// <param name="height"></param>
		/// <returns></returns>
		protected IEnumerable SetHeight(double height)
		{
			this.Creature.Height = (float)height;
			Send.CreatureBodyUpdate(this.Creature);

			yield break;
		}

		/// <summary>
		/// Plays sound effect in rage of AI's creature.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		protected IEnumerable PlaySound(string file)
		{
			Send.PlaySound(this.Creature, file);

			yield break;
		}

		/// <summary>
		/// Adds stat mod to the AI's creature.
		/// </summary>
		/// <param name="stat"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		protected IEnumerable SetStat(Stat stat, float value)
		{
			switch (stat)
			{
				case Stat.Str: this.Creature.StrBase = value; break;
				case Stat.Int: this.Creature.IntBase = value; break;
				case Stat.Dex: this.Creature.DexBase = value; break;
				case Stat.Will: this.Creature.WillBase = value; break;
				case Stat.Luck: this.Creature.LuckBase = value; break;
				default:
					Log.Warning("AI.SetState: Unhandled stat: {0}", stat);
					break;
			}

			yield break;
		}

		/// <summary>
		/// Changes armor in sequence, starting the first item id that
		/// matches the current armor.
		/// </summary>
		/// <example>
		/// itemIds = [15046, 15047, 15048, 15049, 15050]
		/// If current armor is 15046, it's changed to 15047,
		/// if current armor is 15047, it's changed to 15048,
		/// and so on, until there are no more ids.
		/// 
		/// The first id needs to be the default armor, otherwise no
		/// change will occur, since no starting point can be found.
		/// If a creature doesn't have any armor, 0 can be used as the
		/// default, to make it put on armor.
		/// 
		/// Duplicate item ids will not work.
		/// </example>
		/// <param name="itemIds"></param>
		protected IEnumerable SwitchArmor(params int[] itemIds)
		{
			if (itemIds == null || itemIds.Length == 0)
				throw new ArgumentException("A minimum of 1 item id is required.");

			var current = 0;
			var newItemId = -1;

			// Get current item
			var item = this.Creature.Inventory.GetItemAt(Pocket.Armor, 0, 0);
			if (item != null)
				current = item.Info.Id;

			// Search for next item id
			for (int i = 0; i < itemIds.Length - 1; ++i)
			{
				if (itemIds[i] == current)
				{
					newItemId = itemIds[i + 1];
					break;
				}
			}

			// No new id, current not found or end reached
			if (newItemId == -1)
				yield break;

			// Create new item
			Item newItem = null;
			if (newItemId != 0)
			{
				newItem = new Item(newItemId);
				if (item != null)
				{
					// Use same color as the previous armor. Succubi go through
					// more and more revealing clothes, making it look like they
					// lose them, but the colors are variable if we don't set them.
					newItem.Info.Color1 = item.Info.Color1;
					newItem.Info.Color2 = item.Info.Color2;
					newItem.Info.Color3 = item.Info.Color3;
				}
			}

			// Equip new item and remove old one
			if (item != null)
				this.Creature.Inventory.Remove(item);
			if (newItem != null)
				this.Creature.Inventory.Add(newItem, Pocket.Armor);

			yield break;
		}

		/// <summary>
		/// Sets the skin color of the creature controlled by the AI.
		/// </summary>
		/// <param name="skinColor"></param>
		/// <returns></returns>
		protected IEnumerable SetSkinColor(int skinColor)
		{
			this.Creature.SkinColor = (byte)skinColor;

			Send.CreatureFaceUpdate(this.Creature);
			Send.StatUpdate(this.Creature, StatUpdateType.Public, Stat.SkinColor);

			yield break;
		}
		
		/// <summary>
		/// Sets the colors of the creature controlled by the AI.
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		protected IEnumerable SetColor(uint color1, uint color2, uint color3)
		{
			this.Creature.Color1 = color1;
			this.Creature.Color2 = color2;
			this.Creature.Color3 = color3;

			Send.CreatureFaceUpdate(this.Creature);

			yield break;
		}

		/// <summary>
		/// Gives skill to the creature controlled by the AI.
		/// </summary>
		/// <param name="skillIds"></param>
		/// <returns></returns>
		protected IEnumerable AddSkill(SkillId skillId, SkillRank rank)
		{
			this.Creature.Skills.Give(skillId, rank);

			yield break;
		}

		/// <summary>
		/// Removes skills from the creature controlled by the AI.
		/// </summary>
		/// <param name="skillIds"></param>
		/// <returns></returns>
		protected IEnumerable RemoveSkills(params SkillId[] skillIds)
		{
			foreach (var skillId in skillIds)
				this.Creature.Skills.RemoveSilent(skillId);

			yield break;
		}

		/// <summary>
		/// Summons given amount of monsters of given race.
		/// </summary>
		/// <remarks>
		/// Keeps track of the summoned monsters and only summons up to the
		/// given amount.
		/// </remarks>
		/// <param name="raceId"></param>
		/// <param name="amount"></param>
		/// <param name="minDistance"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		protected IEnumerable Summon(int raceId, int amount, int minDistance, int maxDistance)
		{
			// Max amount reached?
			var count = this.GetSummonCount(raceId);
			var diff = amount - count;
			if (diff <= 0)
				yield break;

			// Summon
			var pos = this.Creature.GetPosition();
			var rnd = _rnd;
			for (int i = 0; i < diff; ++i)
			{
				var summonPos = pos.GetRandomInRange(minDistance, maxDistance, _rnd);
				var regionId = this.Creature.RegionId;
				var x = summonPos.X;
				var y = summonPos.Y;

				var creature = ChannelServer.Instance.World.SpawnManager.Spawn(raceId, regionId, x, y, true, true);
				creature.Finish += (_, __) => this.ModifySummonCount(raceId, -1);

				this.ModifySummonCount(raceId, +1);
			}

			yield break;
		}

		/// <summary>
		/// Modifies amount of summons of race id.
		/// </summary>
		/// <param name="raceId"></param>
		/// <param name="modifier"></param>
		/// <returns></returns>
		private int ModifySummonCount(int raceId, int modifier)
		{
			int count;

			lock (_summons)
			{
				if (!_summons.TryGetValue(raceId, out count) || count <= 0)
					count = 0;

				count += modifier;
				_summons[raceId] = count;
			}

			return count;
		}

		/// <summary>
		/// Returns the amount of current summons for race id.
		/// </summary>
		/// <param name="raceId"></param>
		/// <returns></returns>
		private int GetSummonCount(int raceId)
		{
			int count;

			lock (_summons)
			{
				if (!_summons.TryGetValue(raceId, out count) || count <= 0)
					count = 0;
			}

			return count;
		}

		// ------------------------------------------------------------------

		/// <summary>
		/// Called when creature is hit.
		/// </summary>
		/// <param name="action"></param>
		public virtual void OnTargetActionHit(TargetAction action)
		{
			if (this.Creature.Skills.ActiveSkill != null)
			{
				this.SharpMind(this.Creature.Skills.ActiveSkill.Info.Id, SharpMindStatus.Cancelling);
			}

			lock (_reactions)
			{
				var state = _reactions[_state];
				var ev = AiEvent.None;
				var fallback = AiEvent.None;

				// Knock down event
				if (action.Has(TargetOptions.KnockDown) || action.Has(TargetOptions.Smash))
				{
					// Windmill doesn't trigger the knock down event
					if (action.AttackerSkillId != SkillId.Windmill)
					{
						if (action.Has(TargetOptions.Critical))
							ev = AiEvent.CriticalKnockDown;
						else
							ev = AiEvent.KnockDown;
					}
				}
				// Defense event
				else if (action.SkillId == SkillId.Defense)
				{
					ev = AiEvent.DefenseHit;
				}
				// Magic hit event
				// Use skill ids for now, until we know more about what
				// exactly classifies as a magic hit and what doesn't.
				else if (action.AttackerSkillId >= SkillId.Lightningbolt && action.AttackerSkillId <= SkillId.Inspiration)
				{
					ev = AiEvent.MagicHit;
					if (action.Has(TargetOptions.Critical))
						fallback = AiEvent.CriticalHit;
					else
						fallback = AiEvent.Hit;
				}
				// Hit event
				else
				{
					if (action.Has(TargetOptions.Critical))
						ev = AiEvent.CriticalHit;
					else
						ev = AiEvent.Hit;
				}

				// Try to find and execute event
				Dictionary<SkillId, Func<IEnumerable>> evs = null;
				if (state.ContainsKey(ev))
					evs = state[ev];
				else if (state.ContainsKey(fallback))
					evs = state[fallback];

				if (evs != null)
				{
					// Since events can be defined for specific skills,
					// but assumingly still trigger the default events if no
					// skill specific event was defined, we have to check for
					// the specific skill first, and then fall back to "None",
					// for non skill specific events. If both weren't found,
					// we fall through to clear, since only a skill specific
					// event for a different skill was defined, and we still
					// have to reset the current action.

					// Try skill specific event
					if (evs.ContainsKey(action.AttackerSkillId))
					{
						this.SwitchAction(evs[action.AttackerSkillId]);
						return;
					}
					// Try general event
					else if (evs.ContainsKey(SkillId.None))
					{
						this.SwitchAction(evs[SkillId.None]);
						return;
					}
				}

				// Creature was hit, but there's no event

				// If the queue isn't cleared, the AI won't restart the
				// Aggro state, which will make it keep attacking.
				// This also causes a bug, where when you attack a
				// monster while it's attacking you with Smash,
				// it will keep attacking you with Smash, even though
				// the skill was canceled, due to the received hit.
				// The result is a really confusing situation, where
				// normal looking attacks suddenly break through Defense.
				this.Clear();
			}
		}

		/// <summary>
		/// Raised from Creature.Kill when creature died,
		/// before active skill is canceled.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="killer"></param>
		private void OnDeath(Creature creature, Creature killer)
		{
			if (this.Creature.Skills.ActiveSkill != null)
				this.SharpMind(this.Creature.Skills.ActiveSkill.Info.Id, SharpMindStatus.Cancelling);
		}

		/// <summary>
		/// Called when the AI hit someone with a skill.
		/// </summary>
		/// <param name="aAction"></param>
		public void OnUsedSkill(AttackerAction aAction)
		{
			if (this.Creature.Skills.ActiveSkill != null)
				this.ExecuteOnce(this.CompleteSkill());
		}

		// ------------------------------------------------------------------

		protected enum AggroType
		{
			/// <summary>
			/// Stays in Idle unless provoked
			/// </summary>
			Passive,

			/// <summary>
			/// Goes into alert, but doesn't attack unprovoked.
			/// </summary>
			Careful,

			/// <summary>
			/// Goes into alert and attacks if target is in battle mode.
			/// </summary>
			CarefulAggressive,

			/// <summary>
			/// Goes straight into alert and aggro.
			/// </summary>
			Aggressive,
		}

		protected enum AggroLimit
		{
			/// <summary>
			/// Only auto aggroes if no other creature of the same race
			/// aggroed target yet.
			/// </summary>
			One = 1,

			/// <summary>
			/// Only auto aggroes if at most one other creature of the same
			/// race aggroed target.
			/// </summary>
			Two,

			/// <summary>
			/// Only auto aggroes if at most two other creatures of the same
			/// race aggroed target.
			/// </summary>
			Three,

			/// <summary>
			/// Auto aggroes regardless of other enemies.
			/// </summary>
			None = int.MaxValue,
		}

		public enum AiState
		{
			/// <summary>
			/// Doing nothing
			/// </summary>
			Idle,

			/// <summary>
			/// Doing nothing, but noticed a potential target
			/// </summary>
			Aware,

			/// <summary>
			/// Watching target (!)
			/// </summary>
			Alert,

			/// <summary>
			/// Aggroing target (!!)
			/// </summary>
			Aggro,

			/// <summary>
			/// Likes target
			/// </summary>
			Love,
		}

		public enum AiEvent
		{
			None,
			Hit,
			CriticalHit,
			DefenseHit,
			MagicHit,
			KnockDown,
			CriticalKnockDown,
		}
	}

	/// <summary>
	/// Attribute for AI scripts, to specify which races the script is for.
	/// </summary>
	public class AiScriptAttribute : Attribute
	{
		/// <summary>
		/// List of AI names
		/// </summary>
		public string[] Names { get; private set; }

		/// <summary>
		/// New attribute
		/// </summary>
		/// <param name="names"></param>
		public AiScriptAttribute(params string[] names)
		{
			this.Names = names;
		}
	}
}
