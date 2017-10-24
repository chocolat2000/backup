import { GET } from './requester';
import { loadData } from './withData';

const allBackups = serverId => {
  const uri = serverId ? `/api/backups/byserver/${serverId}` : '/api/backups';
  const dataKey = serverId ? `allBackups$${serverId}` : 'allBackups';
  return loadData(dataKey, GET(uri));
};

export { allBackups };
