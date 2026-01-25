using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TestApplication.Models;
using VueJSMVCDotNet;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Endpoints.Model;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication.Handlers
{
    [ModelRoute("/models/mPerson")]
    public class mPersonHandler : IModelHandler<mPerson>
    {
        private static List<mPerson> _persons = new List<mPerson>(new mPerson[]{
            new mPerson("Bob","Loblaw"),
            new mPerson("Fred","Flinston"),
            new mPerson("Barney","Rumble")
        });
        ValueTask<mPerson> IModelHandler<mPerson>.LoadAsync(string id)
        {
            mPerson ret = null;
            foreach (mPerson per in _persons)
            {
                if (id==per.id)
                {
                    ret=per;
                    break;
                }
            }
            return ValueTask.FromResult(ret);
        }

        [ModelLoadAllMethod()]
        public List<mPerson> LoadAll(ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            return _persons;
        }

        [ModelDeleteMethod()]
        public bool Delete(ISecureSession session, [ModelIDParameter()] string id)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            bool ret = false;
            for (int x = 0; x<_persons.Count; x++)
            {
                if (_persons[x].id==id)
                {
                    _persons.RemoveAt(x);
                    ret=true;
                    break;
                }
            }
            return ret;
        }

        [ModelUpdateMethod()]
        public bool Update(ISecureSession session, [ModelInstanceParameter()] mPerson person)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            bool ret = false;
            for (int x = 0; x<_persons.Count; x++)
            {
                if (_persons[x].id==person.id)
                {
                    _persons.RemoveAt(x);
                    _persons.Insert(x, person);
                    ret=true;
                    break;
                }
            }
            return ret;
        }

        [ModelSaveMethod()]
        public string Save(ISecureSession session, [ModelInstanceParameter()] mPerson person)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            _persons.Add(person);
            return person.id;
        }

        [ModelListMethod(true)]
        [FeatureGate("PersonSearchAllowed")]
        public PagedResult<mPerson> Search(string? q, [PageStartIndexParameter()] int pageStartIndex, [PageSizeParameter()] int pageSize, ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            List<mPerson> ret = new List<mPerson>();
            var totalPages = 0;
            if (q != null)
            {
                q = q.ToLower();
                List<mPerson> matches = new List<mPerson>();
                for (int x = 0; x < _persons.Count; x++)
                {
                    if (_persons[x].FirstName.ToLower().Contains(q) ||
                    _persons[x].LastName.ToLower().Contains(q))
                    {
                        matches.Add(_persons[x]);
                    }
                }
                totalPages = (int)Math.Ceiling(matches.Count / (decimal)pageSize);
                for (int x = 0; x < pageSize; x++)
                {
                    if (pageStartIndex + x >= matches.Count)
                    {
                        break;
                    }
                    ret.Add(matches[pageStartIndex + x]);
                }
            }
            return new(ret, totalPages);
        }

        [ExposedMethod(false)]
        public string GetFullName(ISessionManager session, [ModelInstanceParameter()] mPerson person)
        {
            System.Diagnostics.Debug.WriteLine(session.Start);
            return $"{person.LastName}, {person.FirstName}";
        }

        [ExposedMethod(true)]
        public mPerson TestNull()
        {
            return null;
        }

        [ExposedMethod(false)]
        public bool IsGuid(Guid id)
        {
            return true;
        }

        [ExposedMethod(false)]
        public bool AreGuids(ISessionManager session, Guid[] guids)
        {
            return true;
        }

        [ModelListMethod(false)]
        public List<mPerson> ByGuid(Guid id)
        {
            return _persons;
        }

        [ExposedMethod(isSlow: true, arrayElementType: typeof(int))]
        public void SlowStatic(AddItem addCall)
        {
            int idx = 0;
            while (idx<10)
            {
                System.Threading.Thread.Sleep(1000);
                addCall(idx, false);
                idx++;
            }
            addCall(idx, true);
        }

        [ExposedMethod(allowNullResponse: false, isSlow: true)]
        public string GetSlowTimespan()
        {
            DateTime now = DateTime.UtcNow;
            System.Threading.Thread.Sleep(3456);
            return string.Format("This call took {0} ms to complete", DateTime.UtcNow.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethod(allowNullResponse: false, arrayElementType: typeof(string))]
        public void GenerateNames(AddItem addCall, [ModelInstanceParameter()] mPerson person)
        {
            for (int x = 0; x<3; x++)
            {
                switch (x)
                {
                    case 0:
                        addCall(person.FirstName, false);
                        break;
                    case 1:
                        addCall(person.LastName, false);
                        break;
                    case 2:
                        addCall($"{person.LastName}, {person.FirstName}", false);
                        break;
                }
                System.Threading.Thread.Sleep(1000);
            }
            addCall($"{person.LastName} {person.FirstName}", true);
        }

        [ExposedMethod()]
        public string ReadFile(IFormFile contentFile)
        {
            var reader = new StreamReader(contentFile.OpenReadStream());
            var result = $"{contentFile.Name} = {reader.ReadToEnd()}";
            return result;
        }

        [EventStreamMethod()]
        public async Task EventCounter(int count, ChannelWriter<object> writer, CancellationToken cancellationToken)
        {
            try
            {
                for (int i = 0; i < count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteAsync(_persons[i % _persons.Count], cancellationToken);
                    await Task.Delay(1000, cancellationToken);
                }

                Console.WriteLine("Writer completed normally.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Writer was canceled (likely due to client disconnect).");
            }
            finally
            {
                writer.TryComplete();
            }
        }
    }
}
