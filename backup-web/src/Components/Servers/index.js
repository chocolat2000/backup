import React from 'react';
import { Switch, Route } from 'react-router-dom';

import ServerSummary from './Summary';
import ServersDetails from './Details';
import ServersBackup from './../Backup/Server';
import ServersAdd from './Add';

const Servers = () => (
  <Switch>
    <Route exact path="/servers" component={ServerSummary} />
    <Route path="/servers/details/:id" component={ServersDetails} />
    <Route path="/servers/backup/:id" component={ServersBackup} />
    <Route path="/servers/add" component={ServersAdd} />
  </Switch>
);

export default Servers;
