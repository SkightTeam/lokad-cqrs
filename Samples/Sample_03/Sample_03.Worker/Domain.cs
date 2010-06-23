﻿using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Lokad;
using Lokad.Cqrs;
using Lokad.Cqrs.Default;
using NHibernate;
using NHibernate.Linq;
using System.Linq;

namespace Sample_03.Worker
{
	public sealed class CreateAccountFromTimeToTime : IScheduledTask
	{
		readonly ISession _session;
		readonly IMessageClient _client;

		public CreateAccountFromTimeToTime(ISession session, IMessageClient client)
		{
			_session = session;
			_client = client;
		}

		public TimeSpan Execute()
		{
			var account = new AccountEntity();
			_session.Save(account);


			var balanceEntity = new BalanceEntity()
				{
					Change = 0,
					Total = 0,
					Name = "New balance",
					Account = account
				};

			_session.Save(balanceEntity);

			
			Trace.WriteLine("Inserted");


			_client.Send(new AddSomeBonusMessage(account.Id));

			return 10.Seconds();
		}
	}
	[DataContract]
	public sealed class AddSomeBonusMessage : IMessage
	{
		[DataMember]
		public Guid AccountId { get; private set; }

		public AddSomeBonusMessage(Guid accountId)
		{
			AccountId = accountId;
		}

		public override string ToString()
		{
			// helper for better trace logs
			return string.Format("AddSomeBonus to " + AccountId.ToReadable());
		}
	}

	static class StringUtil
	{
		public static string ToReadable(this Guid guid)
		{
			return guid.ToString().Substring(0, 6);
		}
	}

	public sealed class AddSomeBonusHandler : IConsume<AddSomeBonusMessage>
	{
		readonly ISession _session;
		readonly IMessageClient _client;

		public AddSomeBonusHandler(ISession session, IMessageClient client)
		{
			_session = session;
			_client = client;
		}

		public void Consume(AddSomeBonusMessage message)
		{
			SystemUtil.Sleep(1.Seconds());
			var balance = _session
				.Linq<BalanceEntity>()
				.Where(e => e.Account.Id == message.AccountId)
				.OrderByDescending(e => e.Id)
				.Take(1)
				.FirstOrDefault();

			if (balance == null)
			{
				// we've got message in queue from the previous run of 
				// this sample app. ignore it,
				Trace.WriteLine("Account no longer exists. Ignore it");
				return;
			}

			if (balance.Total > 50)
			{
				Trace.WriteLine(string.Format(
					"ENOUGH. Account {0} has too much money: {1}", 
					message.AccountId.ToReadable(), 
					balance.Total));
				return;
			}

			var total = balance.Total + 10;
			var bonus = new BalanceEntity()
				{
					Account = balance.Account,
					Change = 10,
					Name = "Bonus",
					Total = total
				};

			Trace.WriteLine(string.Format(
				"Account {0} - adding {1} bonus with new total {2}", 
				message.AccountId.ToReadable(), 10, total));

			_session.Save(bonus);
			_client.Send(new AddSomeBonusMessage(message.AccountId));
		}
	}
}