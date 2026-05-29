import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Home } from './pages/Home';
import { Planner } from './pages/Planner';
import { TripDetails } from './pages/TripDetails';

function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/planner" element={<Planner />} />
          <Route path="/trip/:id" element={<TripDetails />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}

export default App;
