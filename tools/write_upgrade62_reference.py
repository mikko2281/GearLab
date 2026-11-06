"""Reference for the current PTK model and its exact production source hashes."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
data={
 'Version':'6.2.0',
 'Units':{'length':'mm','torque':'N m','speed':'rpm','angle':'radian'},
 'Sources':[{'Title':'TrashRobotics: rolling-element wave reducer 1:17','URL':'https://www.youtube.com/watch?v=c73QGWpfhQw'},
            {'Title':'Author profile calculator (reference for dimensions)','URL':'https://codeberg.org/TrashRobotics/BallsWaveGearingGenerator'}],
 'Implementation':'Independent normal-envelope construction of the ball-centre trajectory. Single wave; rigid cam and ring; output is the radial separator.',
 'Default':{'Ratio':17,'Balls':17,'RingPockets':18,'BallDiameter_mm':6,'Eccentric_mm':1.2,'ProfileRadius_mm':38},
 'Formulas':{
  'definitions':'i = ball count; z = i+1; b = ball diameter/2; e = eccentricity; c = R-e-b',
  'centre_locus':'S=sqrt(c^2-e^2*sin(z*theta)^2); rho=e*cos(z*theta)+S',
  'derivative':'rho_prime=-e*z*sin(z*theta)*(1+e*cos(z*theta)/S)',
  'normal':'v=(sin(theta),cos(theta)); t=(cos(theta),-sin(theta)); n=(rho*v-rho_prime*t)/sqrt(rho^2+rho_prime^2)',
  'fixed_track':'P=rho*v+(b+clearance)*n',
  'balls':'theta_k=2*pi*k/i+phase/i; u=theta_k+phase; radius=e*cos(u)+sqrt(c^2-e^2*sin(u)^2)',
  'cam':'centre=(-e*sin(phase),e*cos(phase)); radius=R-e-2*b',
  'output':'angle_out=-phase/i; rpm_out=-rpm_in/i; torque_out=torque_in*i*efficiency',
  'bearing_cage':'ideal concentric stationary outer race: cage gain=0.5*inner_gain*(1-ball_diameter/pitch_diameter)'
 },
 'Geometry':{'Preview':'C# parametric mesh; no CAD worker startup','STEP':'analytic spheres and revolved race sections, extrusions and lofts via OCCT','Separator':'one connected solid with radial windows, flange and output shaft','Bearings':'inner/outer grooved races, spherical balls, crown cage base and fingers'},
 'Limits':['No contact pressure, fatigue, wear, stiffness or strength rating.','The cam/ring envelope is sampled; manufacturing tolerances require verification.','Bearing envelopes are parametric sizes, not certified catalog load ratings.','Worm, bevel, RV, hypoid and globoid surfaces retain the approximation limits described in the catalog.'],
 'SourceSHA256':{}
}
for folder in ['src','kernel']:
 for p in sorted((root/folder).rglob('*')):
  if p.is_file() and p.suffix in {'.cs','.py'} and 'vendor' not in p.parts and '__pycache__' not in p.parts:
   data['SourceSHA256'][p.relative_to(root).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
(root/'docs/ENGINEERING_6.2.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Source snapshots:',len(data['SourceSHA256']))
