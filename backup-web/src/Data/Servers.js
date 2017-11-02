import { GET, POST, PUT, DELETE } from './requester';
import { loadData } from './withData';

const allServers = () => {
  return loadData('allServers', GET('/api/servers'));
};

const serverDetails = (serverId, refresh) => {
  return loadData(
    `serverDetails$${serverId}`,
    GET(`/api/servers/${serverId}?refresh=${refresh ? 'true' : 'false'}`)
  );
};

const getDrives = serverId => {
  return GET(`/api/servers/${serverId}/drives`);
};

const getContent = (serverId, path) => {
  const uriPath = encodeURIComponent(path);
  return GET(`/api/servers/${serverId}/content?folder=${uriPath}`);
};

const addServer = server => {
  return POST(`/api/servers/${server.type}`, server);
};

const updateServer = server => {
  return PUT(`/api/servers/${server.id}`, server);
};

const deleteServer = serverId => {
  return DELETE(`/api/servers/${serverId}`).then(result => {
    const { dataKey, data } = allServers();
    loadData(dataKey, data);
    return result;
  });
};

export {
  allServers,
  serverDetails,
  getDrives,
  getContent,
  addServer,
  updateServer,
  deleteServer
};
