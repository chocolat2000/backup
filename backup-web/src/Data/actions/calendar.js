import { GET, POST, PUT, DELETE } from '../requester';

export const backupNow = (server, items) => dispatch => {
  POST('/api/calendar', {
    server,
    items,
    firstrun: new Date().toISOString(),
    periodicity: 'None'
  });
};
