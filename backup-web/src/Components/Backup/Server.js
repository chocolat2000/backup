import React from 'react';

import { withData } from '../../Data/withData';
import { serverDetails } from '../../Data/Servers';
import VMWare from './VMware';
import Windows from './Windows';

const Server = props => {
  const { data: { isLoading, server } } = props;
  if (isLoading || !server) {
    return (
      <section className="section">
        <div className="container">
          <i className="fa fa-spinner fa-pulse fa-3x fa-fw" />
        </div>
      </section>
    );
  }

  switch (server.type) {
    case 'VMware':
      return <VMWare {...props} />;
    case 'Windows':
      return <Windows {...props} />;
    default:
      return <div />;
  }
};

export default withData(Server, ({ match }) => {
  return { server: serverDetails(match.params.id) };
});
