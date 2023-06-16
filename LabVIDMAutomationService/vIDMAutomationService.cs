using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

using System.Messaging;
using System.Transactions;

using LabVIDMAutomationService.Controllers;

namespace LabVIDMAutomationService
{
    public partial class vIDMAutomationService : ServiceBase
    {
        public static MessageQueue myQueue;

        public vIDMAutomationService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Testing: Remove when finished
            System.Threading.Thread.Sleep(10000);

            base.OnStart(args);

            // Don't pull the queue and start handling requests until we've authenticated with the API
            vIDMController.OnVIDMAuthenticated += new vIDMController.vIDMAuthenticated(OnVIDMAuthenticated);
            vIDMController.Initalize();

            //vIDMQueueController.ProcessVIDMQueue();
        }

        protected void OnVIDMAuthenticated()
        {
            InitalizeQueue();
            vIDMQueueController.ProcessVIDMQueue();
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        protected void InitalizeQueue()
        {
            myQueue = new MessageQueue(".\\private$\\vidmqueue");
            myQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });

            myQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(vIDMAutomationMessageQueue_ReceiveCompleted);
            myQueue.BeginReceive();
        }

        protected void vIDMAutomationMessageQueue_ReceiveCompleted(object sender, System.Messaging.ReceiveCompletedEventArgs e)
        {
            try
            {
                MessageQueue mq = (MessageQueue)sender;
                Message m = mq.EndReceive(e.AsyncResult);
                
                vIDMQueueController.ProcessVIDMQueue();

                mq.BeginReceive();
            }
            catch (MessageQueueException) { }
        }
    }
}
