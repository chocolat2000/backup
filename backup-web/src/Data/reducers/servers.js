import objectAssignDeep from 'object-assign-deep';
import {
  REQUEST_SERVER,
  REQUEST_UPDATE_SERVER,
  RECEIVE_UPDATE_SERVER,
  RECEIVE_SERVER,
  REQUEST_SERVERS,
  RECEIVE_SERVERS,
  RECEIVE_FOLDER
} from '../actions/severs';

const splitName = name => {
  const splited = name.split('\\');
  if (splited.length === 2 && splited[1] === '') return [name];

  let prevVal = '';
  return splited.reduce((result, val, idx) => {
    if (val !== '') {
      if (prevVal === '') {
        prevVal = `${val}\\`;
      } else {
        if (idx === 1) {
          prevVal = `${prevVal}${val}`;
        } else {
          prevVal = `${prevVal}\\${val}`;
        }
      }
      result.push(prevVal);
    }
    return result;
  }, []);
};

const server = (
  state = { isFetching: false, isUpdating: false, serverfolders: {} },
  action
) => {
  switch (action.type) {
    case REQUEST_SERVER:
      return Object.assign({}, state, { isFetching: true });
    case RECEIVE_SERVER:
      return Object.assign({}, state, { isFetching: false }, action.server);
    case REQUEST_UPDATE_SERVER:
      return Object.assign({}, state, { isUpdating: true });
    case RECEIVE_UPDATE_SERVER:
      return Object.assign({}, state, { isUpdating: false }, action.server);
    case RECEIVE_FOLDER: {
      const { content, folder } = action;
      const { folders, files } = content;
      const { serverfolders } = state;

      const folderPath = folder && splitName(folder);
      if (!folderPath || folderPath.length === 0) {
        return Object.assign({}, state, {
          isFetching: false,
          serverfolders: Object.assign(
            {},
            serverfolders,
            Object.assign(...folders.map(f => ({ [f]: { name: f } })))
          )
        });
      }

      const patchObject = folderPath.reduceRight((result, f, idx) => {
        let name = f.substr(f.lastIndexOf('\\') + 1);
        if (name === '') name = f;
        if (idx === folderPath.length - 1) {
          return {
            [f]: {
              name,
              folders: folders.reduce((result, folder) => {
                const name = folder.substr(folder.lastIndexOf('\\') + 1);
                result[folder] = { name };
                return result;
              }, {}),
              files: files.reduce((result, file) => {
                const name = file.substr(file.lastIndexOf('\\') + 1);
                result[file] = { name };
                return result;
              }, {})
            }
          };
        }
        return {
          [f]: {
            folders: result
          }
        };
      }, {});

      return objectAssignDeep({}, state, {
        isFetching: false,
        serverfolders: patchObject
      });
    }
    default:
      return state;
  }
};

const servers = (state = { isFetching: false, list: {} }, action) => {
  switch (action.type) {
    case REQUEST_SERVERS:
      return Object.assign({}, state, { isFetching: true, list: {} });
    case RECEIVE_SERVERS: {
      const list = Object.assign({}, ...action.list.map(s => ({ [s.id]: s })));
      return Object.assign({}, state, { isFetching: false, list });
    }
    case REQUEST_SERVER:
    case REQUEST_UPDATE_SERVER:
    case RECEIVE_UPDATE_SERVER:
    case RECEIVE_SERVER:
    case RECEIVE_FOLDER: {
      const s = Object.assign({}, server(state.list[action.serverId], action));
      const list = Object.assign({}, state.list, { [action.serverId]: s });
      return Object.assign({}, state, { list });
    }
    default:
      return state;
  }
};

export default servers;
