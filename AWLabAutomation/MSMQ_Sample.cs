using System.Messaging;
using System.Transactions;

static void SendMessage()
{
	using (TransactionScope transaction = new TransactionScope())
	{
		using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:192.168.20.20\private$\f5automationqueue"))
		{
			Message message = new Message();
			message.Label = "F5 Automation Notification";
			queue.Send(message, MessageQueueTransactionType.Single);
		}
		transaction.Complete();
	}
}