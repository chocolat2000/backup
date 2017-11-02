import { GET, DELETE } from './requester';
import { loadData } from './withData';

const allBackups = function(serverId) {
  const uri = serverId ? `/api/backups/byserver/${serverId}` : '/api/backups';
  const dataKey = serverId ? `allBackups$${serverId}` : 'allBackups';
  return loadData(dataKey, GET(uri));
};

const cancel = function(backupId) {
  return DELETE(`/api/backups/${backupId}`);
};

export { allBackups, cancel };
