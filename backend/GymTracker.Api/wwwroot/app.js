const app = document.querySelector('#app');
const modal = document.querySelector('#modal');
const modalContent = document.querySelector('#modalContent');
const pageTitle = document.querySelector('#pageTitle');
const eyebrow = document.querySelector('#eyebrow');

const state = {
  stats: null, exercises: [], exerciseResponse: null, plans: [], sessions: [], weights: [],
  profileKey: localStorage.getItem('gymtracker-profile') || crypto.randomUUID(),
  route: location.hash.slice(1) || 'dashboard', searchTimer: null
};
localStorage.setItem('gymtracker-profile', state.profileKey);

const titles = {
  dashboard: ['TODAY', 'Your training overview'], exercises: ['LIBRARY', 'Find your next movement'],
  plans: ['PROGRAMMING', 'Workout plans'], sessions: ['TRAINING LOG', 'Completed workouts'],
  progress: ['PROGRESS', 'Track the work']
};
const esc = value => String(value ?? '').replace(/[&<>'"]/g, char => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[char]));
const fmtDate = value => new Intl.DateTimeFormat('en', {month:'short', day:'numeric', year:'numeric'}).format(new Date(value));
const initials = name => name.split(/\s+/).slice(0,2).map(word => word[0]).join('').toUpperCase();

async function api(path, options = {}) {
  const response = await fetch(path, {
    ...options,
    headers: {'Content-Type':'application/json', 'X-Profile-Key':state.profileKey, ...(options.headers || {})}
  });
  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `Request failed (${response.status})`);
  }
  return response.status === 204 ? null : response.json();
}

async function loadCore() {
  const [stats, plans, sessions, weights] = await Promise.all([
    api('/api/dashboard/stats'), api('/api/workoutplans'), api('/api/workoutsessions'), api('/api/bodyweights')
  ]);
  Object.assign(state, {stats, plans, sessions, weights});
}

async function loadExercises(search = '', filters = {}) {
  const query = new URLSearchParams({search, englishOnly:'true', ...filters});
  state.exerciseResponse = await api(`/api/exercises/discover?${query}`);
  state.exercises = state.exerciseResponse.exercises;
}

function updateNavigation() {
  const [overline, title] = titles[state.route] || titles.dashboard;
  eyebrow.textContent = overline; pageTitle.textContent = title;
  document.querySelectorAll('[data-route]').forEach(link => link.classList.toggle('active', link.dataset.route === state.route));
  document.body.classList.remove('menu-open');
}

async function route() {
  state.route = location.hash.slice(1) || 'dashboard';
  if (!titles[state.route]) state.route = 'dashboard';
  updateNavigation();
  app.innerHTML = '<div class="loading"><span></span>Loading your training space…</div>';
  try {
    if (!state.stats) await loadCore();
    if (state.route === 'exercises' && !state.exerciseResponse) await loadExercises();
    render();
  } catch (error) { renderError(error); }
}

function render() {
  ({dashboard:renderDashboard, exercises:renderExercises, plans:renderPlans, sessions:renderSessions, progress:renderProgress}[state.route] || renderDashboard)();
}

function renderDashboard() {
  const s = state.stats;
  const latest = state.sessions[0];
  app.innerHTML = `
    <section class="hero">
      <article class="hero-main"><p class="section-kicker">BUILD CONSISTENCY</p><h2>Every session is a vote for the <em>stronger you.</em></h2><p>Plan deliberately, log the work, and let small improvements compound.</p><button class="primary" data-action="quick-session">Log today's workout</button></article>
      <article class="hero-side"><div><small>LATEST BODY WEIGHT</small><div class="metric">${s.latestWeightKg ?? '—'}${s.latestWeightKg ? '<small> kg</small>' : ''}</div><p>${s.latestWeightKg ? 'Keep showing up.' : 'Add your first check-in.'}</p></div><button class="secondary" data-action="add-weight">Add check-in</button></article>
    </section>
    <section class="stat-grid">
      ${statCard('Exercise library', s.totalExercises)}${statCard('Workout plans', s.totalWorkoutPlans)}${statCard('Sessions logged', s.totalWorkoutSessions)}${statCard('Minutes last session', latest?.durationMinutes ?? '—')}
    </section>
    <section class="grid-two">
      <article class="card"><div class="card-header"><h2>Recent training</h2><button class="text-button" data-go="sessions">View log →</button></div>${sessionList(state.sessions.slice(0,4))}</article>
      <article class="card"><div class="card-header"><h2>Ready-made plans</h2><button class="text-button" data-go="plans">View plans →</button></div>${planMiniList(state.plans.slice(0,4))}</article>
    </section>`;
}

function statCard(label, value) { return `<article class="stat-card"><span>${esc(label)}</span><strong>${esc(value)}</strong></article>`; }
function sessionList(items) {
  if (!items.length) return empty('No workouts yet', 'Log your first session and it will appear here.');
  return `<div class="list">${items.map(item => `<div class="list-item"><span class="list-icon">${new Date(item.completedOn).getDate()}</span><div class="list-copy"><strong>${esc(item.name)}</strong><small>${fmtDate(item.completedOn)} · ${item.durationMinutes} min · ${item.exercises?.length ?? item.exerciseCount} exercises</small></div></div>`).join('')}</div>`;
}
function planMiniList(items) {
  if (!items.length) return empty('No plans yet', 'Create a reusable workout plan.');
  return `<div class="list">${items.map(plan => `<div class="list-item"><span class="list-icon">${plan.exercises.length}</span><div class="list-copy"><strong>${esc(plan.name)}</strong><small>${esc(plan.description || 'Custom training plan')}</small></div></div>`).join('')}</div>`;
}
function empty(title, copy) { return `<div class="empty"><div><b>${esc(title)}</b><div>${esc(copy)}</div></div></div>`; }

function renderExercises() {
  const r = state.exerciseResponse;
  const facetOptions = (items, selected) => `<option value="">All</option>${items.slice(0,30).map(x => `<option value="${esc(x.value)}" ${x.value === selected ? 'selected' : ''}>${esc(x.value)} (${x.count})</option>`).join('')}`;
  app.innerHTML = `
    <div class="notice">Browse ${r.totalResults} English-language exercises imported from the open wger exercise library. Save favorites for faster planning.</div>
    <div class="toolbar"><label class="search"><input id="exerciseSearch" placeholder="Search exercises, muscles, equipment…"></label><select id="bodyPartFilter" aria-label="Body part">${facetOptions(r.bodyParts)}</select><select id="equipmentFilter" aria-label="Equipment">${facetOptions(r.equipments)}</select></div>
    <div id="exerciseResults">${exerciseCards(r.exercises)}</div>`;
}

function exerciseCards(items) {
  if (!items.length) return empty('No matches', 'Try a broader search or clear the filters.');
  return `<section class="exercise-grid">${items.slice(0,48).map(ex => `<article class="exercise-card">
    <div class="exercise-visual">${ex.imageUrl ? `<img src="${esc(ex.imageUrl)}" alt="" loading="lazy">` : `<span>${esc(initials(ex.name))}</span>`}</div>
    <div class="exercise-body"><h3>${esc(ex.name)}</h3><div class="meta">${[ex.bodyPart,ex.equipment,ex.category].filter(Boolean).slice(0,3).map(x=>`<span class="pill">${esc(x)}</span>`).join('')}</div><div class="exercise-actions"><button class="text-button" data-action="exercise-detail" data-id="${ex.id}">Details</button><button class="favorite ${ex.isFavorite?'on':''}" data-action="favorite" data-id="${ex.id}" aria-label="Toggle favorite">${ex.isFavorite?'★':'☆'}</button></div></div>
  </article>`).join('')}</section>${items.length > 48 ? `<p style="color:var(--muted);text-align:center">Showing the first 48 of ${items.length} results. Search to narrow the list.</p>` : ''}`;
}

function renderPlans() {
  app.innerHTML = `<div class="section-heading"><div><p class="section-kicker">REPEAT WHAT WORKS</p><h2>${state.plans.length} training plans</h2></div><button class="primary" data-action="add-plan">+ New plan</button></div>
    ${state.plans.length ? `<section class="plan-grid">${state.plans.map(plan => `<article class="plan-card"><span class="pill">${plan.exercises.length} exercises</span><h3>${esc(plan.name)}</h3><p>${esc(plan.description || 'A focused training session.')}</p><div class="plan-exercises">${plan.exercises.slice(0,5).map(ex=>`<div class="plan-exercise"><span>${esc(ex.exerciseName)}</span><b>${ex.targetSets} × ${ex.targetReps}</b></div>`).join('')}</div><div class="row-between"><button class="secondary" data-action="log-plan" data-id="${plan.id}">Log this workout</button><button class="danger" data-action="delete-plan" data-id="${plan.id}">Delete</button></div></article>`).join('')}</section>` : empty('Build your first plan','Choose exercises, targets, and a goal for the session.')}`;
}

function renderSessions() {
  app.innerHTML = `<div class="section-heading"><div><p class="section-kicker">THE WORK ADDS UP</p><h2>${state.sessions.length} completed sessions</h2></div><button class="primary" data-action="quick-session">+ Log workout</button></div>
    ${state.sessions.length ? `<section class="session-table">${state.sessions.map(session => {const d=new Date(session.completedOn);return `<article class="session-row"><div class="date-box"><small>${d.toLocaleString('en',{month:'short'}).toUpperCase()}</small><strong>${d.getDate()}</strong></div><div><strong>${esc(session.name)}</strong><p>${session.exercises.length} exercises · ${session.durationMinutes} minutes${session.notes?` · ${esc(session.notes)}`:''}</p></div><button class="secondary" data-action="session-detail" data-id="${session.id}">Details</button><button class="danger" data-action="delete-session" data-id="${session.id}">Delete</button></article>`}).join('')}</section>` : empty('Your log is ready','Complete a workout and record the sets that moved you forward.')}`;
}

function renderProgress() {
  const ordered = [...state.weights].sort((a,b)=>new Date(a.date)-new Date(b.date));
  const values = ordered.map(x=>Number(x.weightKg)); const min = Math.min(...values, 0); const max = Math.max(...values, 1);
  app.innerHTML = `<div class="section-heading"><div><p class="section-kicker">MEASURE THE TREND</p><h2>Body-weight history</h2></div><button class="primary" data-action="add-weight">+ Add check-in</button></div>
    <section class="grid-two"><article class="card"><div class="card-header"><h2>Weight trend</h2><span class="pill">${ordered.length} entries</span></div>${ordered.length ? `<div class="chart">${ordered.slice(-16).map(x=>`<div class="bar" style="height:${35+((Number(x.weightKg)-min)/(max-min||1))*150}px" title="${x.weightKg} kg"><span>${new Date(x.date).toLocaleDateString('en',{month:'short',day:'numeric'})}</span></div>`).join('')}</div>` : empty('No trend yet','Add two or more check-ins to see your progress.')}</article><article class="card"><div class="card-header"><h2>Check-ins</h2></div>${ordered.length ? `<div class="list">${[...ordered].reverse().slice(0,8).map(x=>`<div class="list-item"><span class="list-icon">↗</span><div class="list-copy"><strong>${x.weightKg} kg</strong><small>${fmtDate(x.date)}</small></div><button class="danger" data-action="delete-weight" data-id="${x.id}">Delete</button></div>`).join('')}</div>` : empty('Start the record','One honest check-in is enough to begin.')}</article></section>`;
}

function pickerRow(mode, selected = {}) {
  const isPlan = mode === 'plan';
  return `<div class="exercise-picker" data-picker><div class="field"><label>Exercise</label><select name="exerciseId" required>${state.exercises.map(x=>`<option value="${x.id}" ${x.id==selected.exerciseId?'selected':''}>${esc(x.name)}</option>`).join('')}</select></div><div class="field"><label>Sets</label><input name="sets" type="number" min="1" value="${selected.sets || selected.targetSets || 3}" required></div><div class="field"><label>Reps</label><input name="reps" type="number" min="1" value="${selected.reps || selected.targetReps || 10}" required></div>${isPlan?'':`<div class="field"><label>Kg</label><input name="weight" type="number" min="0" step="0.5" value="${selected.weightKg || 0}" required></div>`}<button type="button" class="icon-button" data-action="remove-picker" aria-label="Remove exercise">×</button></div>`;
}

async function ensureExercises() { if (!state.exercises.length) await loadExercises(); }
async function openSessionModal(planId) {
  await ensureExercises(); const plan = state.plans.find(x=>x.id==planId);
  const rows = plan?.exercises?.map(x=>pickerRow('session',x)).join('') || pickerRow('session');
  showModal(`<h2 id="modalTitle">Log a workout</h2><p class="intro">Record the session while the numbers are fresh.</p><form id="sessionForm"><div class="form-grid"><div class="field"><label>Session name</label><input name="name" value="${esc(plan?.name || 'Training session')}" required></div><div class="field"><label>Date</label><input name="date" type="date" value="${new Date().toISOString().slice(0,10)}" required></div><div class="field"><label>Duration (minutes)</label><input name="duration" type="number" min="1" value="45" required></div><div class="field"><label>Notes</label><input name="notes" placeholder="How did it feel?"></div></div><div id="pickerRows">${rows}</div><button type="button" class="text-button" data-action="add-session-exercise">+ Add exercise</button><div class="form-actions"><button type="button" class="secondary" data-action="close-modal">Cancel</button><button class="primary">Save workout</button></div></form>`);
}

async function openPlanModal() {
  await ensureExercises();
  showModal(`<h2 id="modalTitle">Create workout plan</h2><p class="intro">Turn a good session into a repeatable program.</p><form id="planForm"><div class="form-grid"><div class="field"><label>Plan name</label><input name="name" placeholder="Upper body strength" required></div><div class="field"><label>Description</label><input name="description" placeholder="Goal or training focus"></div></div><div id="pickerRows">${pickerRow('plan')}</div><button type="button" class="text-button" data-action="add-plan-exercise">+ Add exercise</button><div class="form-actions"><button type="button" class="secondary" data-action="close-modal">Cancel</button><button class="primary">Create plan</button></div></form>`);
}

function openWeightModal() {
  showModal(`<h2 id="modalTitle">Body-weight check-in</h2><p class="intro">Focus on the trend, not one number.</p><form id="weightForm"><div class="form-grid"><div class="field"><label>Date</label><input name="date" type="date" value="${new Date().toISOString().slice(0,10)}" required></div><div class="field"><label>Weight (kg)</label><input name="weight" type="number" min="1" step="0.1" placeholder="75.5" required></div></div><div class="form-actions"><button type="button" class="secondary" data-action="close-modal">Cancel</button><button class="primary">Save check-in</button></div></form>`);
}

function showModal(html){ modalContent.innerHTML=html; modal.hidden=false; document.body.style.overflow='hidden'; }
function closeModal(){ modal.hidden=true; modalContent.innerHTML=''; document.body.style.overflow=''; }
function toast(message){ const el=document.querySelector('#toast'); el.textContent=message; el.classList.add('show'); setTimeout(()=>el.classList.remove('show'),2600); }
function renderError(error){ app.innerHTML=`<div class="error-state"><strong>GymTracker could not connect.</strong><p>${esc(error.message)}</p><button class="secondary" data-action="retry">Try again</button></div>`; }

document.addEventListener('click', async event => {
  const target = event.target.closest('[data-action],[data-go]');
  if (!target) return;
  if (target.dataset.go) { location.hash=target.dataset.go; return; }
  const action=target.dataset.action, id=target.dataset.id;
  try {
    if(action==='quick-session') await openSessionModal();
    if(action==='log-plan') await openSessionModal(id);
    if(action==='add-plan') await openPlanModal();
    if(action==='add-weight') openWeightModal();
    if(action==='close-modal') closeModal();
    if(action==='add-session-exercise') document.querySelector('#pickerRows').insertAdjacentHTML('beforeend',pickerRow('session'));
    if(action==='add-plan-exercise') document.querySelector('#pickerRows').insertAdjacentHTML('beforeend',pickerRow('plan'));
    if(action==='remove-picker' && document.querySelectorAll('[data-picker]').length>1) target.closest('[data-picker]').remove();
    if(action==='favorite') { const ex=state.exercises.find(x=>x.id==id); await api(`/api/exercises/${id}/favorite`,{method:ex.isFavorite?'DELETE':'POST'}); ex.isFavorite=!ex.isFavorite; target.textContent=ex.isFavorite?'★':'☆'; target.classList.toggle('on',ex.isFavorite); }
    if(action==='exercise-detail') { const ex=state.exercises.find(x=>x.id==id); showModal(`<h2 id="modalTitle">${esc(ex.name)}</h2><p class="intro">${esc(ex.bodyPart||'Exercise')} · ${esc(ex.equipment||'No equipment listed')}</p>${ex.imageUrl?`<div class="exercise-visual" style="height:260px;border-radius:14px"><img src="${esc(ex.imageUrl)}" alt=""></div>`:''}<p style="line-height:1.7;color:var(--muted)">${esc(ex.description||'No description is available for this exercise yet.')}</p>`); }
    if(action==='session-detail') { const x=state.sessions.find(v=>v.id==id); showModal(`<h2 id="modalTitle">${esc(x.name)}</h2><p class="intro">${fmtDate(x.completedOn)} · ${x.durationMinutes} minutes</p><div class="list">${x.exercises.map(e=>`<div class="list-item"><div class="list-copy"><strong>${esc(e.exerciseName)}</strong><small>${e.sets} sets × ${e.reps} reps · ${e.weightKg} kg</small></div></div>`).join('')}</div>`); }
    if(action==='delete-plan') await remove(`/api/workoutplans/${id}`,'plan');
    if(action==='delete-session') await remove(`/api/workoutsessions/${id}`,'workout');
    if(action==='delete-weight') await remove(`/api/bodyweights/${id}`,'check-in');
    if(action==='retry'){state.stats=null;await route();}
  } catch(error){toast(error.message);}
});

document.addEventListener('submit', async event => {
  event.preventDefault(); const form=event.target;
  try {
    if(form.id==='weightForm') { const f=new FormData(form); await api('/api/bodyweights',{method:'POST',body:JSON.stringify({date:new Date(f.get('date')).toISOString(),weightKg:Number(f.get('weight'))})}); }
    if(form.id==='planForm') { const f=new FormData(form), rows=[...form.querySelectorAll('[data-picker]')]; await api('/api/workoutplans',{method:'POST',body:JSON.stringify({name:f.get('name'),description:f.get('description'),exercises:rows.map((r,i)=>({exerciseId:Number(r.querySelector('[name=exerciseId]').value),orderIndex:i+1,targetSets:Number(r.querySelector('[name=sets]').value),targetReps:Number(r.querySelector('[name=reps]').value)}))})}); }
    if(form.id==='sessionForm') { const f=new FormData(form), rows=[...form.querySelectorAll('[data-picker]')]; await api('/api/workoutsessions',{method:'POST',body:JSON.stringify({name:f.get('name'),completedOn:new Date(`${f.get('date')}T12:00:00`).toISOString(),durationMinutes:Number(f.get('duration')),notes:f.get('notes'),exercises:rows.map(r=>({exerciseId:Number(r.querySelector('[name=exerciseId]').value),sets:Number(r.querySelector('[name=sets]').value),reps:Number(r.querySelector('[name=reps]').value),weightKg:Number(r.querySelector('[name=weight]').value)}))})}); }
    closeModal(); state.stats=null; await loadCore(); render(); toast('Saved successfully');
  } catch(error){toast(error.message);}
});

document.addEventListener('input', event => {
  if(event.target.id!=='exerciseSearch') return;
  clearTimeout(state.searchTimer); state.searchTimer=setTimeout(()=>refreshExerciseResults(),350);
});
document.addEventListener('change', event => { if(['bodyPartFilter','equipmentFilter'].includes(event.target.id)) refreshExerciseResults(); });
async function refreshExerciseResults(){
  try{ const filters={}; const body=document.querySelector('#bodyPartFilter')?.value, equipment=document.querySelector('#equipmentFilter')?.value; if(body)filters.bodyPart=body;if(equipment)filters.equipment=equipment; await loadExercises(document.querySelector('#exerciseSearch')?.value||'',filters); document.querySelector('#exerciseResults').innerHTML=exerciseCards(state.exercises); }catch(error){toast(error.message);}
}
async function remove(path,label){ await api(path,{method:'DELETE'}); state.stats=null; await loadCore(); render(); toast(`${label[0].toUpperCase()+label.slice(1)} deleted`); }

document.querySelector('#menuButton').addEventListener('click',()=>document.body.classList.toggle('menu-open'));
modal.addEventListener('click',event=>{if(event.target===modal)closeModal();});
window.addEventListener('keydown',event=>{if(event.key==='Escape')closeModal();});
window.addEventListener('hashchange',route);
route();
