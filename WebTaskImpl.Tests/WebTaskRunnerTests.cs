using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using WebTasksImpl.Impl;
using WebTasksImpl.Models;

namespace WebTaskImpl.Tests
{
    [TestFixture]
    public class WebTaskRunnerTests
    {
        private TaskScheduler _defaultTplScheduler;

        private WebTaskRunner _runner;

        [SetUp]
        public void SetUp()
        {
            SetDefaultTplScheduler();

           _runner = new WebTaskRunner();
        }

        private void SetSynchronousSchedulerForTpl()
        {
            // TPL task are running synchronously.
            // http://www.jarrodstormo.com/2012/02/05/unit-testing-when-using-the-tpl/> 
            var testTaskScheduler = new CurrentThreadTaskScheduler();
            Type taskSchedulerType = typeof (TaskScheduler);
            FieldInfo defaultTaskSchedulerField = taskSchedulerType.GetField("s_defaultTaskScheduler",
                                                                             BindingFlags.SetField | BindingFlags.Static |
                                                                             BindingFlags.NonPublic);
            if (_defaultTplScheduler == null)
            {
                _defaultTplScheduler = defaultTaskSchedulerField.GetValue(null) as TaskScheduler; 
            }

            defaultTaskSchedulerField.SetValue(null, testTaskScheduler);
        }

        private  void SetDefaultTplScheduler()
        {
            if (_defaultTplScheduler != null)
            {
                Type taskSchedulerType = typeof(TaskScheduler);
                FieldInfo defaultTaskSchedulerField = taskSchedulerType.GetField("s_defaultTaskScheduler",
                                                                                 BindingFlags.SetField | BindingFlags.Static |
                                                                                 BindingFlags.NonPublic);
                defaultTaskSchedulerField.SetValue(null, _defaultTplScheduler);
            }
        }

        [Test]
        public void RunWebTask_Success()
        {
            SetSynchronousSchedulerForTpl();

            Guid webTaskId = _runner.RunWebTask(() => new TestWebTaskResult { Foo = "Test" });
        
            WebTaskStatus webTaskStatus =  _runner.GetWebTaskStatus(webTaskId);

            Assert.AreEqual(WebTaskState.Finished, webTaskStatus.State);
            Assert.IsInstanceOf<TestWebTaskResult>(webTaskStatus.Result);
            Assert.AreEqual("Test", ((TestWebTaskResult)webTaskStatus.Result).Foo);
        }

        [Test]
        public void RunWebTask_Failed()
        {
            SetSynchronousSchedulerForTpl();

            Guid webTaskId = _runner.RunWebTask(() =>
            {
                throw new DivideByZeroException();
            });

            WebTaskStatus webTaskStatus = _runner.GetWebTaskStatus(webTaskId);

            Assert.AreEqual(WebTaskState.Failed, webTaskStatus.State);
            Assert.IsNull(webTaskStatus.Result);
            Assert.AreEqual(new DivideByZeroException().Message ,webTaskStatus.Error);
        }

        [Test]
        public void RunWebTask_Canceled()
        {
            ManualResetEventSlim mEvent = new ManualResetEventSlim(false);
            
            Guid webTaskId = _runner.RunWebTask((context) =>
            {
                mEvent.Wait();

                context.CancellationToken.ThrowIfCancellationRequested();

                return new TestWebTaskResult { Foo = "Test" };
            });

            _runner.CancelTask(webTaskId);

            mEvent.Set();

            // Gives some time to cancel -> not determistics. 
            Thread.Sleep(1000);
            WebTaskStatus webTaskStatus = _runner.GetWebTaskStatus(webTaskId);

            Assert.AreEqual(WebTaskState.Canceled, webTaskStatus.State);
            Assert.IsNull(webTaskStatus.Result);
        }

        [Test]
        public void RunWebTask_MoreWebTask_Success()
        {
           const int taskCount = 10;
           Guid[] taskIds = new Guid[taskCount];
           ManualResetEventSlim mEvent = new ManualResetEventSlim(false);

           foreach (var foo in Enumerable.Range(0, taskCount))
           {
               Guid webTaskId = _runner.RunWebTask((context) =>
               {
                   // Last webtask signal event.
                   if (foo == taskCount - 1)
                   {
                       mEvent.Set();
                   }

                   return new TestWebTaskResult { Foo = foo.ToString() };
               });

               taskIds[foo] = webTaskId;
           }

           // We will wait for the last webtask when is finished. 
           mEvent.Wait();
           Thread.Sleep(1000);

           for (int i = 0; i < taskIds.Length; i++)
           {
               WebTaskStatus status = _runner.GetWebTaskStatus(taskIds[i]);
               
               Assert.AreEqual(WebTaskState.Finished, status.State);
               Assert.IsInstanceOf<TestWebTaskResult>(status.Result);
               Assert.AreEqual(i.ToString(),((TestWebTaskResult)status.Result).Foo);
           }

        }

        [Test]
        public void Progress_WebTaskFinished_ProgressesSavedInResult()
        {
            SetSynchronousSchedulerForTpl();

            int progressesCount = new Random().Next(0, 100);

            Guid webTaskId = _runner.RunWebTask(context =>
            {
                for (int i = 1; i <= progressesCount; i++)
                {
                    context.ReportProgress(new WebTaskProgress()
                    {
                        Current = i,
                        Total = progressesCount,
                        Message = "Progress " + i
                    });
                }

                return new TestWebTaskResult {Foo = "Test"};
            });

            WebTaskStatus webTaskStatus = _runner.GetWebTaskStatus(webTaskId);

            Assert.AreEqual(webTaskStatus.State, WebTaskState.Finished);
            Assert.IsNotNull(webTaskStatus.Progresses);
            Assert.AreEqual(progressesCount, webTaskStatus.Progresses.Length);
            
            for (int i = 0; i < progressesCount; i++)
            {
                WebTaskProgress progressUnderTest = webTaskStatus.Progresses[i];

                Assert.AreEqual(progressesCount, progressUnderTest.Total);
                Assert.AreEqual(i + 1, progressUnderTest.Current);
                Assert.AreEqual("Progress " + (i + 1), progressUnderTest.Message);
            }
        }

        [Test]
        public void Progress_WebTaskRunning_ProgressReportedInStatus()
        {
            ManualResetEventSlim mEvent = new ManualResetEventSlim(false);

            Guid webTaskId = _runner.RunWebTask(context =>
            {
                context.ReportProgress(new WebTaskProgress {Current = 1, Total = 2, Message = "Half done!"});

               mEvent.Wait();

                return new TestWebTaskResult { Foo = "Test" };
            });

            Thread.Sleep(1000); // Give some time to webtask for reporting progress.
            WebTaskStatus webTaskStatusInProgress = _runner.GetWebTaskStatus(webTaskId);
            mEvent.Set();

            Assert.AreEqual(webTaskStatusInProgress.State, WebTaskState.Running);
            Assert.IsNotNull(webTaskStatusInProgress.Progresses);
            Assert.AreEqual(1, webTaskStatusInProgress.Progresses.Length);
            Assert.AreEqual(1, webTaskStatusInProgress.Progresses[0].Current);
            Assert.AreEqual(2, webTaskStatusInProgress.Progresses[0].Total);
            Assert.AreEqual("Half done!", webTaskStatusInProgress.Progresses[0].Message);  
        }

        [Test]
        public void GetWebTaskStatus_WebTaskFinished_ResultRemovesAfterPickedOut()
        {
            SetSynchronousSchedulerForTpl();

            Guid webTaskId = _runner.RunWebTask(context =>
            {
                return new TestWebTaskResult { Foo = "Test" };
            });
        
            WebTaskStatus webTaskStatus1 = _runner.GetWebTaskStatus(webTaskId);
            WebTaskStatus webTaskStatus2 = _runner.GetWebTaskStatus(webTaskId);

            Assert.IsNotNull(webTaskStatus1);
            Assert.IsNull(webTaskStatus2);
        }

        [Test]
        public void GetWebTaskStatus_PersistResultEnabled_ResultPersistAfterPickedOut()
        {
            SetSynchronousSchedulerForTpl();

            Guid webTaskId = _runner.RunWebTask(context =>
            {
                return new TestWebTaskResult { Foo = "Test" };
            }, new WebTaskConfiguration() { PersistResultWhenPickedOut = true});

            WebTaskStatus webTaskStatus1 = _runner.GetWebTaskStatus(webTaskId);
            WebTaskStatus webTaskStatus2 = _runner.GetWebTaskStatus(webTaskId);

            Assert.IsNotNull(webTaskStatus1);
            Assert.IsNotNull(webTaskStatus2);
        }

        [Test]
        public void ResultExpiration_ResultDontPickedUp_ExpiredResultDeleted()
        {
            // Sets cleaning interval.
            _runner = new WebTaskRunner(TimeSpan.FromMilliseconds(10));
            
            Guid webTaskId = _runner.RunWebTask(context =>
            {
                return new TestWebTaskResult { Foo = "Test" };
            }, new WebTaskConfiguration() { ResultExpiration = TimeSpan.FromMilliseconds(1) });

            Thread.Sleep(100);

            WebTaskStatus webTaskStatus = _runner.GetWebTaskStatus(webTaskId);

            Assert.IsNull(webTaskStatus);

        }
    }


    public class TestWebTaskResult : WebTaskResult
    {
        public string Foo { get; set; }
    }
}
