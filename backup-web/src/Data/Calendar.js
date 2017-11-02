import { GET, POST } from './requester';

const backupNow = function(server, items) {
  return POST('/api/calendar', {
    server,
    items,
    firstrun: new Date().toISOString(),
    periodicity: 'None'
  });
};

export { backupNow };
