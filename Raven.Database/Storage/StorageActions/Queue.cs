using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace Raven.Database.Storage.StorageActions
{
    public partial class DocumentStorageActions 
    {
        public void EnqueueToQueue(string name, byte[] data)
        {
            using (var update = new Update(session, Queue, JET_prep.Insert))
            {
                Api.SetColumn(session, Queue, tableColumnsCache.QueueColumns["name"], name, Encoding.Unicode);
                Api.SetColumn(session, Queue, tableColumnsCache.QueueColumns["data"], data);

                update.Save();
            }
        }

        public IEnumerable<Tuple<byte[], int>> PeekFromQueue(string name)
        {
            Api.JetSetCurrentIndex(session, Queue,"by_name");
            Api.MakeKey(session, Queue, name, Encoding.Unicode, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(session, Queue, SeekGrbit.SeekEQ) == false)
                yield break;
            Api.MakeKey(session, Queue, name, Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSetIndexRange(session, Queue, SetIndexRangeGrbit.RangeInclusive | SetIndexRangeGrbit.RangeUpperLimit);
            do
            {
                var currentName = Api.RetrieveColumnAsString(session, Queue, tableColumnsCache.QueueColumns["name"]);
                if(currentName!=name)
                    continue;
                if (Api.EscrowUpdate(session, Queue, tableColumnsCache.QueueColumns["read_count"],1) > 5)
                {
                    // read too much, probably poison message, remove it
                    Api.JetDelete(session, Queue);
                    continue;
                }
                yield return new Tuple<byte[], int>(
                    Api.RetrieveColumn(session, Queue, tableColumnsCache.QueueColumns["data"]),
                    Api.RetrieveColumnAsInt32(session, Queue, tableColumnsCache.QueueColumns["id"]).Value);
            } while (Api.TryMoveNext(session, Queue));
        }

        public void DeleteFromQueue(int id)
        {
            Api.JetSetCurrentIndex(session, Queue, "by_id");
            Api.MakeKey(session, Queue, id, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(session, Queue, SeekGrbit.SeekEQ) == false)
                return;
            Api.JetDelete(session, Queue);
        }
    }
}