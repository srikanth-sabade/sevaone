import { FormEvent, useRef, useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Archive, BookOpen, CheckCircle2, Edit3, FileSpreadsheet, History, Layers3, Plus, RotateCcw, Search, ShieldCheck, Sparkles, Upload } from 'lucide-react'
import { toast } from 'sonner'
import { govAdminApi } from '../api'
import { Modal } from '../components/Modal'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'
import { PageIntro, QueryState, formatNumber } from '../components/AnalyticsPrimitives'
import { useCatalogAdminChanges, useCatalogAdminDepartments, useCatalogAdminServices, useCatalogAdminSubDepartments } from '../hooks/useCatalogManagement'
import { parseCatalogueImport, type CatalogueImportPreview } from '../lib/catalogueImport'
import type { CatalogAdminDepartment, CatalogAdminService, CatalogAdminServiceWriteRequest, CatalogAdminSubDepartment, CatalogImportBatch, CatalogImportValidationRow } from '../types'

type Tab = 'departments' | 'services' | 'sub-departments' | 'history'
type SortBy = 'name' | 'displayOrder' | 'active'
type DepartmentDraft = { code: string; name: string; departmentNameKannada: string; departmentUrl: string; departmentUrlKannada: string; displayOrder: number }
type SubDepartmentDraft = { departmentCode: string; name: string; nameKannada: string; displayOrder: number; isActive: boolean }
type ServiceListRow = { departmentCode: string; departmentName: string; serviceCode: string; name: string; nameKannada: string | null; category: string | null; isActive: boolean; version: number }
type PendingToggle = { kind: 'department'; item: CatalogAdminDepartment } | { kind: 'service'; item: ServiceListRow } | { kind: 'sub-department'; item: CatalogAdminSubDepartment } | null

const emptyDepartment: DepartmentDraft = { code: '', name: '', departmentNameKannada: '', departmentUrl: '', departmentUrlKannada: '', displayOrder: 0 }
const emptySubDepartment: SubDepartmentDraft = { departmentCode: '', name: '', nameKannada: '', displayOrder: 0, isActive: true }

type ServiceDraft = CatalogAdminServiceWriteRequest

function emptyService(departmentCode = ''): ServiceDraft {
  return {
    departmentCode, serviceCode: '', name: '', nameKannada: '', subDepartmentName: '', shortDescription: '', shortDescriptionKannada: '',
    description: '', descriptionKannada: '', isSakalaEnabled: false, sakalaLabelRaw: '', sakalaDays: 0, departmentFee: null, serviceCharge: null,
    documents: [], documentsKannada: [], eligibility: [], eligibilityKannada: [], aliases: [], steps: [], stepsKannada: [], jurisdiction: '',
    category: '', modeOfService: 'Online', departmentServiceId: '', portal: '', serviceUrl: '', deeplinkCitizenUrl: '', deeplinkCitizenUrlKannada: '',
    deeplinkOperatorUrl: '', homepageUrl: '', integrationMethod: '', sevasindhuServiceId: '', remarks: '', isActive: true, visibleToRoles: ['citizen'],
  }
}

function fromService(service: CatalogAdminService): ServiceDraft {
  const { id: _id, departmentName: _departmentName, version: _version, ...draft } = service
  return draft
}

function listText(values: string[]) { return values.join('\n') }
function parseList(value: string) { return value.split(/[\n,]/).map((part) => part.trim()).filter(Boolean) }
function dateLabel(value: string) { return new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }

export function CataloguePage() {
  const queryClient = useQueryClient()
  const departmentsQuery = useCatalogAdminDepartments(true)
  const [tab, setTab] = useState<Tab>('departments')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<'all' | 'active' | 'inactive'>('all')
  const [sortBy, setSortBy] = useState<SortBy>('name')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const serviceQuery = useCatalogAdminServices({ departmentCode: departmentFilter || undefined, search: search || undefined, isActive: status === 'all' ? undefined : status === 'active' }, true)
  const subDepartmentQuery = useCatalogAdminSubDepartments({ departmentCode: departmentFilter || undefined, search: search || undefined, isActive: status === 'all' ? undefined : status === 'active' }, true)
  const changesQuery = useCatalogAdminChanges(true)
  const [departmentModal, setDepartmentModal] = useState<'create' | 'edit' | null>(null)
  const [departmentDraft, setDepartmentDraft] = useState<DepartmentDraft>(emptyDepartment)
  const [subDepartmentModal, setSubDepartmentModal] = useState<'create' | 'edit' | null>(null)
  const [subDepartmentDraft, setSubDepartmentDraft] = useState<SubDepartmentDraft>(emptySubDepartment)
  const [editingSubDepartmentId, setEditingSubDepartmentId] = useState<string | null>(null)
  const [serviceModal, setServiceModal] = useState<'create' | 'edit' | null>(null)
  const [serviceDraft, setServiceDraft] = useState<ServiceDraft>(emptyService())
  const [editingServiceKey, setEditingServiceKey] = useState<{ departmentCode: string; serviceCode: string } | null>(null)
  const [pendingToggle, setPendingToggle] = useState<PendingToggle>(null)
  const [busy, setBusy] = useState(false)
  const [importModal, setImportModal] = useState(false)
  const [importPreview, setImportPreview] = useState<CatalogueImportPreview | null>(null)
  const [importBatch, setImportBatch] = useState<CatalogImportBatch | null>(null)
  const [importValidationRows, setImportValidationRows] = useState<CatalogImportValidationRow[]>([])
  const [importBusy, setImportBusy] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const activeDepartments = departmentsQuery.data?.filter((department) => department.isActive).length ?? 0
  const activeServices = serviceQuery.data?.filter((service) => service.isActive).length ?? 0
  const departments = departmentsQuery.data ?? []
  const serviceRows = serviceQuery.data ?? []
  const subDepartmentRows = subDepartmentQuery.data ?? []

  const filteredDepartments = useMemo(() => sortDepartments(departments.filter((department) => `${department.code} ${department.name}`.toLowerCase().includes(search.toLowerCase())).filter((department) => status === 'all' || (status === 'active' ? department.isActive : !department.isActive)), sortBy), [departments, search, sortBy, status])
  const filteredServices = useMemo(() => sortServices(serviceRows, sortBy), [serviceRows, sortBy])
  const filteredSubDepartments = useMemo(() => sortSubDepartments(subDepartmentRows, sortBy), [sortBy, subDepartmentRows])

  const tabItems = [
    { id: 'departments' as const, label: 'Departments', icon: Layers3, count: departments.length },
    { id: 'services' as const, label: 'Services', icon: BookOpen, count: serviceRows.length },
    { id: 'sub-departments' as const, label: 'Sub-departments', icon: Layers3, count: subDepartmentRows.length },
    { id: 'history' as const, label: 'Change history', icon: History, count: changesQuery.data?.length ?? 0 },
  ]

  function invalidateCatalogue() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ['gov-admin', 'catalog', 'departments'] }),
      queryClient.invalidateQueries({ queryKey: ['gov-admin', 'catalog', 'services'] }),
      queryClient.invalidateQueries({ queryKey: ['gov-admin', 'catalog', 'sub-departments'] }),
      queryClient.invalidateQueries({ queryKey: ['gov-admin', 'catalog', 'changes'] }),
      queryClient.invalidateQueries({ queryKey: ['gov-admin', 'departments'] }),
    ])
  }

  function openCreateDepartment() { setDepartmentDraft(emptyDepartment); setDepartmentModal('create') }
  function openEditDepartment(department: CatalogAdminDepartment) {
    setDepartmentDraft({ code: department.code, name: department.name, departmentNameKannada: department.departmentNameKannada ?? '', departmentUrl: department.departmentUrl ?? '', departmentUrlKannada: department.departmentUrlKannada ?? '', displayOrder: department.displayOrder })
    setDepartmentModal('edit')
  }
  function openCreateService() { setServiceDraft(emptyService(departmentFilter)); setEditingServiceKey(null); setServiceModal('create') }
  function openCreateSubDepartment() { setSubDepartmentDraft({ ...emptySubDepartment, departmentCode: departmentFilter }); setEditingSubDepartmentId(null); setSubDepartmentModal('create') }
  function openEditSubDepartment(item: CatalogAdminSubDepartment) {
    setSubDepartmentDraft({ departmentCode: item.departmentCode, name: item.name, nameKannada: item.nameKannada ?? '', displayOrder: item.displayOrder, isActive: item.isActive })
    setEditingSubDepartmentId(item.id)
    setSubDepartmentModal('edit')
  }

  async function openEditService(departmentCode: string, serviceCode: string) {
    setBusy(true)
    try {
      setServiceDraft(fromService(await govAdminApi.getCatalogService(departmentCode, serviceCode)))
      setEditingServiceKey({ departmentCode, serviceCode })
      setServiceModal('edit')
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not load the service.') }
    finally { setBusy(false) }
  }

  async function saveDepartment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!departmentDraft.code.trim() || !departmentDraft.name.trim()) { toast.error('Enter a department code and name.'); return }
    setBusy(true)
    try {
      const body = { ...departmentDraft, code: departmentDraft.code.trim(), name: departmentDraft.name.trim(), departmentNameKannada: departmentDraft.departmentNameKannada.trim() || null, departmentUrl: departmentDraft.departmentUrl.trim() || null, departmentUrlKannada: departmentDraft.departmentUrlKannada.trim() || null }
      if (departmentModal === 'edit') await govAdminApi.updateCatalogDepartment(departmentDraft.code, body)
      else await govAdminApi.createCatalogDepartment(body)
      setDepartmentModal(null); await invalidateCatalogue(); toast.success(departmentModal === 'edit' ? 'Department updated.' : 'Department created and published.')
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not save the department.') }
    finally { setBusy(false) }
  }

  function toggleDepartment(department: CatalogAdminDepartment) {
    if (department.isActive) { setPendingToggle({ kind: 'department', item: department }); return }
    void applyDepartmentToggle(department)
  }

  async function applyDepartmentToggle(department: CatalogAdminDepartment) {
    const action = department.isActive ? 'deactivate' : 'reactivate'
    setBusy(true)
    try { await govAdminApi.setCatalogDepartmentActive(department.code, !department.isActive); await invalidateCatalogue(); toast.success(`Department ${action}d.`) }
    catch (error) { toast.error(error instanceof Error ? error.message : 'Could not update the department.') }
    finally { setBusy(false) }
  }

  async function saveService(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!serviceDraft.departmentCode.trim() || !serviceDraft.serviceCode.trim() || !serviceDraft.name.trim()) { toast.error('Enter a department, service code, and service name.'); return }
    setBusy(true)
    try {
      const body: ServiceDraft = { ...serviceDraft, departmentCode: serviceDraft.departmentCode.trim(), serviceCode: serviceDraft.serviceCode.trim(), name: serviceDraft.name.trim(), nameKannada: serviceDraft.nameKannada?.trim() || null, subDepartmentName: serviceDraft.subDepartmentName?.trim() || null, shortDescription: serviceDraft.shortDescription?.trim() || null, shortDescriptionKannada: serviceDraft.shortDescriptionKannada?.trim() || null, description: serviceDraft.description?.trim() || null, descriptionKannada: serviceDraft.descriptionKannada?.trim() || null, category: serviceDraft.category?.trim() || null, jurisdiction: serviceDraft.jurisdiction?.trim() || null, modeOfService: serviceDraft.modeOfService || null, departmentServiceId: serviceDraft.departmentServiceId?.trim() || null, portal: serviceDraft.portal?.trim() || null, serviceUrl: serviceDraft.serviceUrl?.trim() || null, deeplinkCitizenUrl: serviceDraft.deeplinkCitizenUrl?.trim() || null, deeplinkCitizenUrlKannada: serviceDraft.deeplinkCitizenUrlKannada?.trim() || null, deeplinkOperatorUrl: serviceDraft.deeplinkOperatorUrl?.trim() || null, homepageUrl: serviceDraft.homepageUrl?.trim() || null, integrationMethod: serviceDraft.integrationMethod?.trim() || null, sevasindhuServiceId: serviceDraft.sevasindhuServiceId?.trim() || null, remarks: serviceDraft.remarks?.trim() || null }
      if (serviceModal === 'edit' && editingServiceKey) await govAdminApi.updateCatalogService(editingServiceKey.departmentCode, editingServiceKey.serviceCode, body)
      else await govAdminApi.createCatalogService(body)
      setServiceModal(null); await invalidateCatalogue(); toast.success(serviceModal === 'edit' ? 'Service updated.' : 'Service created and published.')
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not save the service.') }
    finally { setBusy(false) }
  }

  async function saveSubDepartment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!subDepartmentDraft.departmentCode.trim() || !subDepartmentDraft.name.trim()) { toast.error('Choose a department and enter a sub-department name.'); return }
    setBusy(true)
    try {
      const body = { ...subDepartmentDraft, departmentCode: subDepartmentDraft.departmentCode.trim(), name: subDepartmentDraft.name.trim(), nameKannada: subDepartmentDraft.nameKannada.trim() || null }
      if (subDepartmentModal === 'edit' && editingSubDepartmentId) await govAdminApi.updateCatalogSubDepartment(editingSubDepartmentId, body)
      else await govAdminApi.createCatalogSubDepartment(body)
      setSubDepartmentModal(null); await invalidateCatalogue(); toast.success(subDepartmentModal === 'edit' ? 'Sub-department updated.' : 'Sub-department created and published.')
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not save the sub-department.') }
    finally { setBusy(false) }
  }

  function toggleService(service: ServiceListRow) {
    if (service.isActive) { setPendingToggle({ kind: 'service', item: service }); return }
    void applyServiceToggle(service)
  }

  async function applyServiceToggle(service: ServiceListRow) {
    const action = service.isActive ? 'deactivate' : 'reactivate'
    setBusy(true)
    try { await govAdminApi.setCatalogServiceActive(service.departmentCode, service.serviceCode, !service.isActive); await invalidateCatalogue(); toast.success(`Service ${action}d.`) }
    catch (error) { toast.error(error instanceof Error ? error.message : 'Could not update the service.') }
    finally { setBusy(false) }
  }

  function toggleSubDepartment(item: CatalogAdminSubDepartment) {
    if (item.isActive) { setPendingToggle({ kind: 'sub-department', item }); return }
    void applySubDepartmentToggle(item)
  }

  async function applySubDepartmentToggle(item: CatalogAdminSubDepartment) {
    const action = item.isActive ? 'deactivate' : 'reactivate'
    setBusy(true)
    try { await govAdminApi.setCatalogSubDepartmentActive(item.id, !item.isActive); await invalidateCatalogue(); toast.success(`Sub-department ${action}d.`) }
    catch (error) { toast.error(error instanceof Error ? error.message : 'Could not update the sub-department.') }
    finally { setBusy(false) }
  }

  async function confirmPendingToggle() {
    if (!pendingToggle) return
    if (pendingToggle.kind === 'department') {
      await applyDepartmentToggle(pendingToggle.item)
    } else if (pendingToggle.kind === 'service') {
      await applyServiceToggle(pendingToggle.item)
    } else {
      await applySubDepartmentToggle(pendingToggle.item)
    }
    if (!busy) setPendingToggle(null)
  }

  function openImportModal() {
    setImportPreview(null)
    setImportBatch(null)
    setImportValidationRows([])
    setImportModal(true)
  }

  async function handleImportFile(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    setImportBusy(true)
    try { setImportPreview(await parseCatalogueImport(file)); setImportBatch(null); setImportValidationRows([]) }
    catch (error) { setImportPreview(null); toast.error(error instanceof Error ? error.message : 'Could not read this file.') }
    finally { setImportBusy(false) }
  }

  async function stageImport() {
    if (!importPreview || importPreview.issues.length > 0) return
    setImportBusy(true)
    try {
      const result = await govAdminApi.startCatalogImport({ sourceFilename: importPreview.filename, sourceFormat: importPreview.format, rows: importPreview.rows })
      const [batch, rows] = await Promise.all([govAdminApi.catalogImportStatus(result.batchId), govAdminApi.catalogImportRows(result.batchId)])
      setImportBatch(batch)
      setImportValidationRows(rows)
      toast.success('Import staged for review.')
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not stage this import.') }
    finally { setImportBusy(false) }
  }

  async function publishImport() {
    if (!importBatch || importBatch.errorCount > 0 || importBatch.status !== 'ready_to_publish') return
    setImportBusy(true)
    try {
      const result = await govAdminApi.publishCatalogImport(importBatch.id)
      await invalidateCatalogue()
      toast.success(`${result.published} service(s) published from the import.`)
      setImportModal(false)
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Could not publish this import.') }
    finally { setImportBusy(false) }
  }

  async function discardImport() {
    if (!importBatch) { setImportModal(false); return }
    setImportBusy(true)
    try { await govAdminApi.discardCatalogImport(importBatch.id); setImportModal(false); toast.success('Import discarded. Its audit record was retained.') }
    catch (error) { toast.error(error instanceof Error ? error.message : 'Could not discard this import.') }
    finally { setImportBusy(false) }
  }

  return <>
    <PageIntro eyebrow="Administration" title="Catalogue management" description="Keep departments, sub-departments, and public services accurate, discoverable, and safely auditable across SevaOne." action={<div className="flex flex-wrap gap-2"><Button type="button" variant="outline" onClick={openImportModal} className="h-11 rounded-xl border-[color:var(--gov-blue)]/25 bg-white px-4 text-[color:var(--gov-blue)] hover:bg-[color:var(--gov-blue)]/5"><Upload className="mr-2 h-4 w-4" /> Import services</Button>{tab !== 'history' && <Button type="button" onClick={tab === 'departments' ? openCreateDepartment : tab === 'services' ? openCreateService : openCreateSubDepartment} className="h-11 rounded-xl bg-[color:var(--gov-blue)] px-5 text-white hover:bg-[color:var(--gov-blue-dark)]"><Plus className="mr-2 h-4 w-4" /> Create new {tab === 'departments' ? 'department' : tab === 'services' ? 'service' : 'sub-department'}</Button>}</div>} />
    <Card className="mb-5 overflow-hidden rounded-2xl border-[color:var(--gov-blue)]/15 bg-gradient-to-br from-[color:var(--gov-navy)] to-[color:var(--gov-blue-dark)] p-6 text-white shadow-[0_20px_60px_rgba(22,76,147,0.16)]"><div className="flex flex-wrap items-start justify-between gap-5"><div><div className="flex items-center gap-2 text-xs font-bold uppercase tracking-[0.16em] text-white/65"><ShieldCheck className="h-4 w-4 text-[color:var(--gov-green)]" /> Platform Admin publishing space</div><h2 className="mt-3 max-w-2xl text-2xl font-semibold tracking-tight">One trusted catalogue for every citizen journey.</h2><p className="mt-2 max-w-2xl text-sm leading-6 text-white/70">Every save is transactional, immediately published, and recorded. Deactivation hides an item without deleting the record.</p></div><Sparkles className="hidden h-12 w-12 text-[color:var(--gov-gold)]/80 sm:block" /></div><div className="mt-6 grid gap-3 sm:grid-cols-3"><Stat label="Active departments" value={formatNumber(activeDepartments)} /><Stat label="Visible services" value={formatNumber(activeServices)} /><Stat label="Recent changes" value={formatNumber(changesQuery.data?.length ?? 0)} /></div></Card>
    <div className="mb-5 flex flex-wrap items-center gap-2 rounded-2xl border border-border bg-white/70 p-2">{tabItems.map(({ id, label, icon: Icon, count }) => <button key={id} type="button" onClick={() => setTab(id)} className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold transition ${tab === id ? 'bg-[color:var(--gov-blue)] text-white shadow-sm' : 'text-muted-foreground hover:bg-secondary hover:text-[color:var(--gov-navy)]'}`}><Icon className="h-4 w-4" />{label}<span className={`rounded-full px-2 py-0.5 text-[10px] ${tab === id ? 'bg-white/15 text-white' : 'bg-secondary text-muted-foreground'}`}>{count}</span></button>)}</div>
    {tab !== 'history' && <div className="mb-5 flex flex-wrap gap-3"><div className="relative min-w-[230px] flex-1"><Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" /><Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder={tab === 'departments' ? 'Search departments' : tab === 'services' ? 'Search services or codes' : 'Search sub-departments'} className="h-11 rounded-xl pl-9" /></div>{(tab === 'services' || tab === 'sub-departments') && <select value={departmentFilter} onChange={(event) => setDepartmentFilter(event.target.value)} className="h-11 rounded-xl border border-border bg-white px-3 text-sm text-[color:var(--gov-navy)]"><option value="">All departments</option>{departments.map((department) => <option key={department.code} value={department.code}>{department.code} · {department.name}</option>)}</select>}<select value={status} onChange={(event) => setStatus(event.target.value as typeof status)} className="h-11 rounded-xl border border-border bg-white px-3 text-sm text-[color:var(--gov-navy)]"><option value="all">All statuses</option><option value="active">Active only</option><option value="inactive">Inactive only</option></select><select value={sortBy} onChange={(event) => setSortBy(event.target.value as SortBy)} className="h-11 rounded-xl border border-border bg-white px-3 text-sm text-[color:var(--gov-navy)]"><option value="name">Sort by name</option><option value="displayOrder">{tab === 'services' ? 'Sort by version' : 'Sort by display order'}</option><option value="active">Sort by active status</option></select></div>}
    {tab === 'departments' && <QueryState isLoading={departmentsQuery.isLoading} isError={departmentsQuery.isError}><DepartmentTable departments={filteredDepartments} onEdit={openEditDepartment} onToggle={(department) => void toggleDepartment(department)} /></QueryState>}
    {tab === 'services' && <QueryState isLoading={serviceQuery.isLoading} isError={serviceQuery.isError}><ServiceTable services={filteredServices} onEdit={(service) => void openEditService(service.departmentCode, service.serviceCode)} onToggle={(service) => toggleService(service)} /></QueryState>}
    {tab === 'sub-departments' && <QueryState isLoading={subDepartmentQuery.isLoading} isError={subDepartmentQuery.isError}><SubDepartmentTable subDepartments={filteredSubDepartments} onEdit={openEditSubDepartment} onToggle={toggleSubDepartment} /></QueryState>}
    {tab === 'history' && <QueryState isLoading={changesQuery.isLoading} isError={changesQuery.isError}><HistoryTable changes={changesQuery.data ?? []} /></QueryState>}

    <Modal open={departmentModal !== null} title={departmentModal === 'edit' ? 'Edit department' : 'Create department'} description="Department codes are permanent identifiers. Keep public links current so citizens reach the right department." onClose={() => { if (!busy) setDepartmentModal(null) }}>
      <form className="grid gap-5 sm:grid-cols-2" onSubmit={(event) => void saveDepartment(event)}><Field label="Department code" hint="2-64 letters, numbers, - or _"><Input required disabled={departmentModal === 'edit'} value={departmentDraft.code} onChange={(event) => setDepartmentDraft({ ...departmentDraft, code: event.target.value })} placeholder="REVENUE" /></Field><Field label="English name"><Input required value={departmentDraft.name} onChange={(event) => setDepartmentDraft({ ...departmentDraft, name: event.target.value })} placeholder="Revenue Department" /></Field><Field label="Kannada name"><Input value={departmentDraft.departmentNameKannada} onChange={(event) => setDepartmentDraft({ ...departmentDraft, departmentNameKannada: event.target.value })} /></Field><Field label="Display order"><Input type="number" min="0" value={departmentDraft.displayOrder} onChange={(event) => setDepartmentDraft({ ...departmentDraft, displayOrder: Number(event.target.value) || 0 })} /></Field><Field label="Department URL"><Input type="url" value={departmentDraft.departmentUrl} onChange={(event) => setDepartmentDraft({ ...departmentDraft, departmentUrl: event.target.value })} placeholder="https://department.karnataka.gov.in" /></Field><Field label="Kannada department URL"><Input type="url" value={departmentDraft.departmentUrlKannada} onChange={(event) => setDepartmentDraft({ ...departmentDraft, departmentUrlKannada: event.target.value })} /></Field><div className="flex justify-end gap-3 border-t border-border pt-5 sm:col-span-2"><Button type="button" variant="outline" disabled={busy} onClick={() => setDepartmentModal(null)} className="rounded-xl">Cancel</Button><Button type="submit" disabled={busy} className="rounded-xl bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue-dark)]">{busy ? 'Saving...' : 'Save and publish'}</Button></div></form>
    </Modal>
    <Modal open={serviceModal !== null} title={serviceModal === 'edit' ? 'Edit service' : 'Create service'} description="Changes publish immediately after validation. Use Deactivate when a service should leave the public catalogue without losing its history." size="xl" onClose={() => { if (!busy) setServiceModal(null) }}><ServiceForm draft={serviceDraft} setDraft={setServiceDraft} editing={serviceModal === 'edit'} busy={busy} onSubmit={saveService} onCancel={() => setServiceModal(null)} /></Modal>
    <Modal open={subDepartmentModal !== null} title={subDepartmentModal === 'edit' ? 'Edit sub-department' : 'Create sub-department'} description="Group related services under a department. Deactivation keeps the record and audit history while removing it from active catalogue use." onClose={() => { if (!busy) setSubDepartmentModal(null) }}>
      <form className="grid gap-5 sm:grid-cols-2" onSubmit={(event) => void saveSubDepartment(event)}><Field label="Department"><select required disabled={subDepartmentModal === 'edit'} value={subDepartmentDraft.departmentCode} onChange={(event) => setSubDepartmentDraft({ ...subDepartmentDraft, departmentCode: event.target.value })} className="h-10 w-full rounded-xl border border-border bg-secondary/30 px-3 text-sm"><option value="">Choose a department</option>{departments.filter((department) => department.isActive).map((department) => <option key={department.code} value={department.code}>{department.code} · {department.name}</option>)}</select></Field><Field label="English name"><Input required value={subDepartmentDraft.name} onChange={(event) => setSubDepartmentDraft({ ...subDepartmentDraft, name: event.target.value })} placeholder="Public Health" /></Field><Field label="Kannada name"><Input value={subDepartmentDraft.nameKannada} onChange={(event) => setSubDepartmentDraft({ ...subDepartmentDraft, nameKannada: event.target.value })} /></Field><Field label="Display order"><Input type="number" min="0" value={subDepartmentDraft.displayOrder} onChange={(event) => setSubDepartmentDraft({ ...subDepartmentDraft, displayOrder: Number(event.target.value) || 0 })} /></Field><label className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)] sm:col-span-2"><input type="checkbox" checked={subDepartmentDraft.isActive} onChange={(event) => setSubDepartmentDraft({ ...subDepartmentDraft, isActive: event.target.checked })} className="h-4 w-4 accent-[color:var(--gov-blue)]" /> Visible in public catalogue</label><div className="flex justify-end gap-3 border-t border-border pt-5 sm:col-span-2"><Button type="button" variant="outline" disabled={busy} onClick={() => setSubDepartmentModal(null)} className="rounded-xl">Cancel</Button><Button type="submit" disabled={busy} className="rounded-xl bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue-dark)]">{busy ? 'Saving...' : 'Save and publish'}</Button></div></form>
    </Modal>
    <Modal open={importModal} title="Import services in bulk" description="Upload a CSV or Excel file, review validation results, then publish the whole batch transactionally." size="xl" onClose={() => { if (!importBusy) setImportModal(false) }}>
      <div className="space-y-6">
        {!importBatch && <div className="rounded-2xl border border-dashed border-[color:var(--gov-blue)]/35 bg-[color:var(--gov-blue)]/5 p-6 text-center"><FileSpreadsheet className="mx-auto h-10 w-10 text-[color:var(--gov-blue)]" /><h3 className="mt-3 text-base font-semibold text-[color:var(--gov-navy)]">Choose a catalogue file</h3><p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-muted-foreground">Use the template columns exactly. Lists such as documents and eligibility use semicolons inside one cell.</p><input ref={fileInputRef} type="file" accept=".csv,.xlsx" onChange={(event) => void handleImportFile(event)} className="hidden" /><div className="mt-5 flex flex-wrap justify-center gap-3"><Button type="button" onClick={() => fileInputRef.current?.click()} disabled={importBusy} className="rounded-xl bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue-dark)]"><Upload className="mr-2 h-4 w-4" /> {importBusy ? 'Reading file...' : 'Choose CSV or XLSX'}</Button><a href="/catalogue-service-import-template.csv" download className="inline-flex h-10 items-center rounded-xl border border-border bg-white px-4 text-sm font-semibold text-[color:var(--gov-navy)] hover:bg-secondary">Download CSV template</a></div></div>}
        {importPreview && !importBatch && <div className="space-y-4"><div className="grid gap-3 sm:grid-cols-3"><ImportStat label="Rows found" value={String(importPreview.rows.length)} /><ImportStat label="Local issues" value={String(importPreview.issues.length)} /><ImportStat label="File type" value={importPreview.format.toUpperCase()} /></div>{importPreview.issues.length > 0 && <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"><p className="font-semibold">Fix these rows before staging</p><ul className="mt-2 space-y-1">{importPreview.issues.slice(0, 6).map((issue) => <li key={`${issue.row}-${issue.message}`}>Row {issue.row}: {issue.message}</li>)}{importPreview.issues.length > 6 && <li>And {importPreview.issues.length - 6} more issue(s).</li>}</ul></div>}<div className="overflow-x-auto rounded-2xl border border-border"><table className="w-full min-w-[640px] text-left text-sm"><thead className="bg-secondary/50 text-[10px] font-bold uppercase tracking-[0.12em] text-muted-foreground"><tr><th className="px-4 py-3">Row</th><th className="px-4 py-3">Department</th><th className="px-4 py-3">Service code</th><th className="px-4 py-3">Service name</th></tr></thead><tbody className="divide-y divide-border">{importPreview.rows.slice(0, 5).map((row, index) => <tr key={`${row.department.code}-${row.service.serviceCode}-${index}`}><td className="px-4 py-3 text-muted-foreground">{index + 2}</td><td className="px-4 py-3 font-semibold text-[color:var(--gov-navy)]">{row.department.name || 'Missing'}</td><td className="px-4 py-3 font-mono text-xs">{row.service.serviceCode || 'Missing'}</td><td className="px-4 py-3">{row.service.name || 'Missing'}</td></tr>)}</tbody></table></div><div className="flex justify-end gap-3"><Button type="button" variant="outline" disabled={importBusy} onClick={() => setImportModal(false)} className="rounded-xl">Cancel</Button><Button type="button" disabled={importBusy || importPreview.issues.length > 0} onClick={() => void stageImport()} className="rounded-xl bg-[color:var(--gov-blue)] text-white hover:bg-[color:var(--gov-blue-dark)]">{importBusy ? 'Staging...' : 'Stage for review'}</Button></div></div>}
        {importBatch && <div className="space-y-5"><div className="grid gap-3 sm:grid-cols-4"><ImportStat label="Rows" value={String(importBatch.rowCount)} /><ImportStat label="Will insert/update" value={String(importBatch.rowCount - importBatch.errorCount)} /><ImportStat label="Errors" value={String(importBatch.errorCount)} /><ImportStat label="Status" value={importBatch.status.replaceAll('_', ' ')} /></div>{importBatch.errorCount > 0 && <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"><p className="font-semibold">This batch cannot be published yet.</p><p className="mt-1">Correct the listed source rows and upload the file again. No live catalogue rows were changed.</p></div>}<div className="max-h-72 overflow-auto rounded-2xl border border-border"><div className="divide-y divide-border">{importValidationRows.slice(0, 100).map((row) => <div key={row.id} className="flex flex-wrap items-center gap-3 px-4 py-3 text-sm"><span className="w-12 text-xs text-muted-foreground">#{row.rowNumber}</span><span className="font-mono text-xs text-muted-foreground">{row.departmentCode} · {row.serviceCode}</span><span className="min-w-0 flex-1 font-semibold text-[color:var(--gov-navy)]">{row.serviceName || 'Unnamed service'}</span><Badge variant="secondary" className={row.errors.length ? 'border-red-200 bg-red-50 text-red-800' : ''}>{row.errors.length ? `${row.errors.length} error(s)` : row.action ?? 'Ready'}</Badge></div>)}</div></div><div className="flex flex-wrap justify-end gap-3"><Button type="button" variant="outline" disabled={importBusy} onClick={() => void discardImport()} className="rounded-xl">Discard batch</Button><Button type="button" disabled={importBusy || importBatch.errorCount > 0 || importBatch.status !== 'ready_to_publish'} onClick={() => void publishImport()} className="rounded-xl bg-[color:var(--gov-green)] text-white hover:brightness-95">{importBusy ? 'Publishing...' : 'Publish batch'}</Button></div></div>}
      </div>
    </Modal>
    <Modal open={pendingToggle !== null} title={pendingToggle?.kind === 'department' ? 'Deactivate department?' : pendingToggle?.kind === 'service' ? 'Deactivate service?' : 'Deactivate sub-department?'} description="This action removes the item from the public catalogue without deleting its database record or audit history." onClose={() => { if (!busy) setPendingToggle(null) }}>
      <div className="space-y-5"><div className="rounded-2xl border border-[color:var(--saffron)]/30 bg-[color:var(--saffron)]/10 p-5"><div className="flex items-start gap-3"><Archive className="mt-0.5 h-5 w-5 shrink-0 text-[color:var(--saffron)]" /><div><p className="text-sm font-semibold text-[color:var(--gov-navy)]">{pendingToggle?.item.name}</p><p className="mt-2 text-sm leading-6 text-muted-foreground">{pendingToggle?.kind === 'department' ? `This department has ${pendingToggle.item.serviceCount} service(s). They will no longer be shown through the public catalogue while the department is inactive.` : 'This item will no longer be shown through the public catalogue. Existing application history remains safe.'}</p></div></div></div><div className="flex justify-end gap-3"><Button type="button" variant="outline" disabled={busy} onClick={() => setPendingToggle(null)} className="rounded-xl">Cancel</Button><Button type="button" disabled={busy} onClick={() => void confirmPendingToggle()} className="rounded-xl bg-red-700 text-white hover:bg-red-800">{busy ? 'Deactivating...' : 'Deactivate'}</Button></div></div>
    </Modal>
  </>
}

function Stat({ label, value }: { readonly label: string; readonly value: string }) { return <div className="rounded-xl border border-white/10 bg-white/10 px-4 py-3"><p className="text-xs text-white/60">{label}</p><p className="mt-1 text-xl font-semibold">{value}</p></div> }
function ImportStat({ label, value }: { readonly label: string; readonly value: string }) { return <div className="rounded-2xl border border-border bg-secondary/35 px-4 py-3"><p className="text-[10px] font-bold uppercase tracking-[0.12em] text-muted-foreground">{label}</p><p className="mt-1 text-lg font-semibold capitalize text-[color:var(--gov-navy)]">{value}</p></div> }
function Field({ label, hint, children }: { readonly label: string; readonly hint?: string; readonly children: React.ReactNode }) { return <div className="space-y-2"><div className="flex items-baseline justify-between gap-2"><Label>{label}</Label>{hint && <span className="text-[10px] text-muted-foreground">{hint}</span>}</div>{children}</div> }

function compareActive(left: boolean, right: boolean) { return Number(right) - Number(left) }
function sortDepartments(rows: CatalogAdminDepartment[], sortBy: SortBy) {
  return [...rows].sort((left, right) => sortBy === 'displayOrder' ? left.displayOrder - right.displayOrder : sortBy === 'active' ? compareActive(left.isActive, right.isActive) : left.name.localeCompare(right.name))
}
function sortServices(rows: ServiceListRow[], sortBy: SortBy) {
  return [...rows].sort((left, right) => sortBy === 'displayOrder' ? left.version - right.version : sortBy === 'active' ? compareActive(left.isActive, right.isActive) : left.name.localeCompare(right.name))
}
function sortSubDepartments(rows: CatalogAdminSubDepartment[], sortBy: SortBy) {
  return [...rows].sort((left, right) => sortBy === 'displayOrder' ? left.displayOrder - right.displayOrder : sortBy === 'active' ? compareActive(left.isActive, right.isActive) : left.name.localeCompare(right.name))
}

function DepartmentTable({ departments, onEdit, onToggle }: { readonly departments: CatalogAdminDepartment[]; readonly onEdit: (department: CatalogAdminDepartment) => void; readonly onToggle: (department: CatalogAdminDepartment) => void }) { return <Card className="overflow-hidden rounded-2xl"><div className="grid grid-cols-[1fr_auto] border-b border-border bg-secondary/40 px-5 py-3 text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground sm:grid-cols-[1fr_1.6fr_0.8fr_0.8fr_auto] sm:gap-4"><span>Department</span><span className="hidden sm:block">Public identity</span><span className="hidden sm:block">Services</span><span className="hidden sm:block">Status</span><span>Actions</span></div>{!departments.length ? <EmptyState label="No departments match these filters." /> : <div className="divide-y divide-border">{departments.map((department) => <div key={department.id} className="grid items-center gap-3 px-5 py-4 sm:grid-cols-[1fr_1.6fr_0.8fr_0.8fr_auto] sm:gap-4"><div><p className="font-semibold text-[color:var(--gov-navy)]">{department.name}</p><p className="mt-1 font-mono text-[11px] text-muted-foreground">{department.code}</p></div><div className="hidden min-w-0 sm:block"><p className="truncate text-sm text-muted-foreground">{department.departmentNameKannada || 'Kannada name not supplied'}</p><p className="mt-1 truncate text-xs text-muted-foreground/70">{department.departmentUrl || 'No public URL'}</p></div><p className="hidden text-sm font-semibold text-[color:var(--gov-navy)] sm:block">{department.serviceCount}</p><div className="hidden sm:block"><Status active={department.isActive} /></div><div className="flex justify-end gap-1"><Button variant="ghost" size="sm" onClick={() => onEdit(department)} aria-label={`Edit ${department.name}`}><Edit3 className="h-4 w-4" /></Button><Button variant="ghost" size="sm" onClick={() => onToggle(department)} className={department.isActive ? 'text-red-700 hover:bg-red-50' : 'text-[color:var(--gov-green)]'} aria-label={`${department.isActive ? 'Deactivate' : 'Reactivate'} ${department.name}`}>{department.isActive ? <Archive className="h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}</Button></div></div>)}</div>}</Card> }

function SubDepartmentTable({ subDepartments, onEdit, onToggle }: { readonly subDepartments: CatalogAdminSubDepartment[]; readonly onEdit: (item: CatalogAdminSubDepartment) => void; readonly onToggle: (item: CatalogAdminSubDepartment) => void }) { return <Card className="overflow-hidden rounded-2xl"><div className="grid grid-cols-[1fr_auto] border-b border-border bg-secondary/40 px-5 py-3 text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground sm:grid-cols-[1.1fr_1fr_0.8fr_0.8fr_auto] sm:gap-4"><span>Sub-department</span><span className="hidden sm:block">Department</span><span className="hidden sm:block">Display order</span><span className="hidden sm:block">Status</span><span>Actions</span></div>{!subDepartments.length ? <EmptyState label="No sub-departments match these filters." /> : <div className="divide-y divide-border">{subDepartments.map((item) => <div key={item.id} className="grid items-center gap-3 px-5 py-4 sm:grid-cols-[1.1fr_1fr_0.8fr_0.8fr_auto] sm:gap-4"><div><p className="font-semibold text-[color:var(--gov-navy)]">{item.name}</p><p className="mt-1 text-xs text-muted-foreground">{item.nameKannada || 'Kannada name not supplied'}</p></div><p className="hidden truncate text-sm text-muted-foreground sm:block">{item.departmentName}<span className="ml-2 font-mono text-[10px]">{item.departmentCode}</span></p><p className="hidden text-sm text-muted-foreground sm:block">{item.displayOrder}</p><div className="hidden sm:block"><Status active={item.isActive} /></div><div className="flex justify-end gap-1"><Button variant="ghost" size="sm" onClick={() => onEdit(item)} aria-label={`Edit ${item.name}`}><Edit3 className="h-4 w-4" /></Button><Button variant="ghost" size="sm" onClick={() => onToggle(item)} className={item.isActive ? 'text-red-700 hover:bg-red-50' : 'text-[color:var(--gov-green)]'} aria-label={`${item.isActive ? 'Deactivate' : 'Reactivate'} ${item.name}`}>{item.isActive ? <Archive className="h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}</Button></div></div>)}</div>}</Card> }
function ServiceTable({ services, onEdit, onToggle }: { readonly services: ServiceListRow[]; readonly onEdit: (service: { departmentCode: string; serviceCode: string }) => void; readonly onToggle: (service: ServiceListRow) => void }) { return <Card className="overflow-hidden rounded-2xl"><div className="grid grid-cols-[1fr_auto] border-b border-border bg-secondary/40 px-5 py-3 text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground sm:grid-cols-[1.3fr_1fr_0.8fr_0.8fr_auto] sm:gap-4"><span>Service</span><span className="hidden sm:block">Department</span><span className="hidden sm:block">Version</span><span className="hidden sm:block">Status</span><span>Actions</span></div>{!services.length ? <EmptyState label="No services match these filters." /> : <div className="divide-y divide-border">{services.map((service) => <div key={`${service.departmentCode}-${service.serviceCode}`} className="grid items-center gap-3 px-5 py-4 sm:grid-cols-[1.3fr_1fr_0.8fr_0.8fr_auto] sm:gap-4"><div><p className="font-semibold text-[color:var(--gov-navy)]">{service.name}</p><p className="mt-1 font-mono text-[11px] text-muted-foreground">{service.serviceCode}</p><p className="mt-1 text-xs text-muted-foreground sm:hidden">{service.departmentCode} · v{service.version}</p></div><p className="hidden truncate text-sm text-muted-foreground sm:block">{service.departmentName}</p><p className="hidden text-sm text-muted-foreground sm:block">v{service.version}</p><div className="hidden sm:block"><Status active={service.isActive} /></div><div className="flex justify-end gap-1"><Button variant="ghost" size="sm" onClick={() => onEdit(service)} aria-label={`Edit ${service.name}`}><Edit3 className="h-4 w-4" /></Button><Button variant="ghost" size="sm" onClick={() => onToggle(service)} className={service.isActive ? 'text-red-700 hover:bg-red-50' : 'text-[color:var(--gov-green)]'} aria-label={`${service.isActive ? 'Deactivate' : 'Reactivate'} ${service.name}`}>{service.isActive ? <Archive className="h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}</Button></div></div>)}</div>}</Card> }
function Status({ active }: { readonly active: boolean }) { return <Badge variant="outline" className={active ? 'border-[color:var(--gov-green)]/30 bg-[color:var(--gov-green)]/8 text-[color:var(--gov-green)]' : 'border-border bg-secondary text-muted-foreground'}>{active ? 'Active' : 'Inactive'}</Badge> }
function EmptyState({ label }: { readonly label: string }) { return <div className="px-5 py-14 text-center text-sm text-muted-foreground"><BookOpen className="mx-auto h-7 w-7 text-muted-foreground/40" /><p className="mt-3">{label}</p></div> }
function HistoryTable({ changes }: { readonly changes: Array<{ id: string; entityType: string; departmentCode: string | null; serviceCode: string | null; changedAt: string; changedBy: string; changeType: string }> }) { return <Card className="overflow-hidden rounded-2xl"><div className="border-b border-border bg-secondary/40 px-5 py-3 text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">Immutable catalogue record</div>{!changes.length ? <EmptyState label="No catalogue changes recorded yet." /> : <div className="divide-y divide-border">{changes.map((change) => <div key={change.id} className="flex flex-wrap items-center gap-3 px-5 py-4"><div className={`flex h-9 w-9 items-center justify-center rounded-xl ${change.entityType === 'department' ? 'bg-[color:var(--gov-blue)]/10 text-[color:var(--gov-blue)]' : 'bg-[color:var(--saffron)]/12 text-[color:var(--saffron)]'}`}>{change.entityType === 'department' ? <Layers3 className="h-4 w-4" /> : <BookOpen className="h-4 w-4" />}</div><div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><p className="text-sm font-semibold capitalize text-[color:var(--gov-navy)]">{change.changeType} {change.entityType}</p><Badge variant="secondary" className="font-mono text-[10px]">{change.departmentCode}{change.serviceCode ? ` · ${change.serviceCode}` : ''}</Badge></div><p className="mt-1 text-xs text-muted-foreground">by {change.changedBy} · {dateLabel(change.changedAt)}</p></div><CheckCircle2 className="h-4 w-4 text-[color:var(--gov-green)]" /></div>)}</div>}</Card> }

function ServiceForm({ draft, setDraft, editing, busy, onSubmit, onCancel }: { readonly draft: ServiceDraft; readonly setDraft: React.Dispatch<React.SetStateAction<ServiceDraft>>; readonly editing: boolean; readonly busy: boolean; readonly onSubmit: (event: FormEvent<HTMLFormElement>) => void; readonly onCancel: () => void }) {
  const update = <K extends keyof ServiceDraft>(key: K, value: ServiceDraft[K]) => setDraft((current) => ({ ...current, [key]: value }))
  return <form className="space-y-6" onSubmit={onSubmit}><div className="grid gap-4 sm:grid-cols-3"><Field label="Department code"><Input required disabled={editing} value={draft.departmentCode} onChange={(event) => update('departmentCode', event.target.value)} placeholder="REV" /></Field><Field label="Service code"><Input required disabled={editing} value={draft.serviceCode} onChange={(event) => update('serviceCode', event.target.value)} placeholder="REV-001" /></Field><Field label="Category"><Input value={draft.category ?? ''} onChange={(event) => update('category', event.target.value)} placeholder="Certificates" /></Field></div><div className="grid gap-4 sm:grid-cols-2"><Field label="English service name"><Input required value={draft.name} onChange={(event) => update('name', event.target.value)} /></Field><Field label="Kannada service name"><Input value={draft.nameKannada ?? ''} onChange={(event) => update('nameKannada', event.target.value)} /></Field><Field label="Short description"><Input value={draft.shortDescription ?? ''} onChange={(event) => update('shortDescription', event.target.value)} /></Field><Field label="Sub-department"><Input value={draft.subDepartmentName ?? ''} onChange={(event) => update('subDepartmentName', event.target.value)} placeholder="Optional grouping" /></Field></div><div className="grid gap-4 sm:grid-cols-2"><Field label="Description"><textarea value={draft.description ?? ''} onChange={(event) => update('description', event.target.value)} rows={4} className="w-full resize-y rounded-xl border border-border bg-secondary/30 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-[color:var(--gov-blue)]/30" /></Field><Field label="Kannada description"><textarea value={draft.descriptionKannada ?? ''} onChange={(event) => update('descriptionKannada', event.target.value)} rows={4} className="w-full resize-y rounded-xl border border-border bg-secondary/30 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-[color:var(--gov-blue)]/30" /></Field></div><div className="grid gap-4 sm:grid-cols-4"><Field label="Mode"><select value={draft.modeOfService ?? ''} onChange={(event) => update('modeOfService', event.target.value)} className="h-10 w-full rounded-xl border border-border bg-secondary/30 px-3 text-sm"><option value="">Not specified</option><option value="Online">Online</option><option value="Offline">Offline</option><option value="Both">Both</option></select></Field><Field label="Sakala days"><Input type="number" min="0" value={draft.sakalaDays ?? 0} onChange={(event) => update('sakalaDays', Number(event.target.value) || 0)} /></Field><Field label="Department fee"><Input type="number" min="0" step="0.01" value={draft.departmentFee ?? ''} onChange={(event) => update('departmentFee', event.target.value ? Number(event.target.value) : null)} /></Field><Field label="Service charge"><Input type="number" min="0" step="0.01" value={draft.serviceCharge ?? ''} onChange={(event) => update('serviceCharge', event.target.value ? Number(event.target.value) : null)} /></Field></div><div className="grid gap-4 sm:grid-cols-3"><ListField label="Required documents" value={draft.documents ?? []} onChange={(value) => update('documents', value)} /><ListField label="Eligibility" value={draft.eligibility ?? []} onChange={(value) => update('eligibility', value)} /><ListField label="Application steps" value={draft.steps ?? []} onChange={(value) => update('steps', value)} /></div><div className="grid gap-4 sm:grid-cols-2"><Field label="Service information URL"><Input type="url" value={draft.serviceUrl ?? ''} onChange={(event) => update('serviceUrl', event.target.value)} /></Field><Field label="Citizen apply URL"><Input type="url" value={draft.deeplinkCitizenUrl ?? ''} onChange={(event) => update('deeplinkCitizenUrl', event.target.value)} /></Field><Field label="Operator apply URL"><Input type="url" value={draft.deeplinkOperatorUrl ?? ''} onChange={(event) => update('deeplinkOperatorUrl', event.target.value)} /></Field><Field label="Homepage fallback URL"><Input type="url" value={draft.homepageUrl ?? ''} onChange={(event) => update('homepageUrl', event.target.value)} /></Field></div><div className="flex flex-wrap items-center justify-between gap-3 border-t border-border pt-5"><label className="flex items-center gap-2 text-sm font-semibold text-[color:var(--gov-navy)]"><input type="checkbox" checked={draft.isActive} onChange={(event) => update('isActive', event.target.checked)} className="h-4 w-4 accent-[color:var(--gov-blue)]" /> Visible in public catalogue</label><div className="flex gap-3"><Button type="button" variant="outline" disabled={busy} onClick={onCancel} className="rounded-xl">Cancel</Button><Button type="submit" disabled={busy} className="rounded-xl bg-[color:var(--gov-blue)] px-5 text-white hover:bg-[color:var(--gov-blue-dark)]">{busy ? 'Publishing...' : 'Save and publish'}</Button></div></div></form>
}

function ListField({ label, value, onChange }: { readonly label: string; readonly value: string[]; readonly onChange: (value: string[]) => void }) { const text = useMemo(() => listText(value), [value]); return <Field label={label} hint="One item per line"><textarea value={text} onChange={(event) => onChange(parseList(event.target.value))} rows={4} className="w-full resize-y rounded-xl border border-border bg-secondary/30 px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-[color:var(--gov-blue)]/30" /></Field> }
