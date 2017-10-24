import React from 'react';
import { Link } from 'react-router-dom';

import { withData } from '../../Data/withData';
import { allServers, deleteServer } from '../../Data/Servers';

const Summary = ({ data: { isLoading, servers } }) => {
  const content = isLoading ? (
    <div style={{ marginBottom: '1.5rem' }}>
      <i className="fa fa-spinner fa-pulse fa-3x fa-fw" />
    </div>
  ) : (
    <table className="table is-striped is-hoverable is-fullwidth">
      <thead>
        <tr>
          <th>
            <abbr title="Remove">Rem</abbr>
          </th>
          <th>Name</th>
          <th>Ip / Host</th>
          <th>Type</th>
        </tr>
      </thead>
      <tbody>
        {servers &&
          servers.map(({ id, name, ip, type }) => (
            <tr key={id}>
              <th>
                <button className="delete" onClick={() => deleteServer(id)} />
              </th>
              <td>
                <Link to={`/servers/details/${id}`}>{name}</Link>
              </td>
              <td>{ip}</td>
              <td>{type}</td>
            </tr>
          ))}
      </tbody>
    </table>
  );

  return (
    <section className="section">
      <div className="container">{content}</div>
      <div className="container">
        <div className="control">
          <Link className="button is-primary" to={'/servers/add'}>
            Add
          </Link>
        </div>
      </div>
    </section>
  );
};

export default withData(Summary, () => {
  return { servers: allServers() };
});
