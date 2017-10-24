import React from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';

import { isAuthenticated } from './Data/auth';
import Login from './Components/Login';
import Navbar from './Components/Navbar';
import Home from './Components/Home';
import ServersSummary from './Components/Servers/Summary';
import ServersDetails from './Components/Servers/Details';
import ServersBackup from './Components/Backup/Server';
import ServersAdd from './Components/Servers/Add';

const App = () => (
  <Router>
    <Route
      render={() =>
        !isAuthenticated() ? (
          <Login />
        ) : (
          <div>
            <section className="section">
              <div className="container">
                <h1 className="title is-1">Hello World</h1>
                <p className="subtitle">
                  My first website with <strong>Bulma</strong>!
                </p>
              </div>
            </section>
            <Route component={Navbar} />
            <Route exact path="/" component={Home} />
            <Route exact path="/servers" component={ServersSummary} />
            <Route path="/servers/details/:id" component={ServersDetails} />
            <Route path="/servers/backup/:id" component={ServersBackup} />
            <Route path="/servers/add" component={ServersAdd} />
          </div>
        )}
    />
  </Router>
);

export default App;
