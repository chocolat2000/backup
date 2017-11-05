import React from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';

import EnsureLogin from './Components/EnsureLogin';
import Navbar from './Components/Navbar';
import ServersSummary from './Components/Servers/Summary';
import ServersNavbar from './Components/Servers/Navbar';
import ServersDetails from './Components/Servers/Details';
import ServersBackup from './Components/Backup/Server';
import ServersAdd from './Components/Servers/Add';

const App = () => (
  <EnsureLogin>
    <Router>
      <div>
        <section className="hero is-primary">
          <div className="hero-body">
            <div className="container">
              <h1 className="title">Hero title</h1>
              <h2 className="subtitle">Hero subtitle</h2>
            </div>
          </div>
          <div className="hero-foot">
            <Navbar />
          </div>
        </section>
        <nav className="navbar has-shadow">
          <div className="container">
            <Route path="/servers" component={ServersNavbar} />
          </div>
        </nav>
        <Route exact path="/servers" component={ServersSummary} />
        <Route exact path="/servers/details/:id" component={ServersDetails} />
        <Route exact path="/servers/backup/:id" component={ServersBackup} />
        <Route exact path="/servers/add" component={ServersAdd} />
      </div>
    </Router>
  </EnsureLogin>
);

export default App;
