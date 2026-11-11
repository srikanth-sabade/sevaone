import { Link } from '@tanstack/react-router'
import gokEmblem from '../assets/gok-emblem.webp'
import { useT } from '../hooks/useT'
import { BrandWordmark } from './BrandWordmark'

export function SiteFooter() {
  const { t } = useT()
  return (
    <footer className="mt-20 border-t border-border bg-secondary/40">
      <div className="mx-auto grid max-w-6xl gap-10 px-4 py-14 md:grid-cols-4">
        <div className="md:col-span-1">
          <div className="flex items-center gap-2.5">
            <img
              src={gokEmblem}
              alt={t('emblemAlt')}
              width={36}
              height={36}
              className="h-9 w-9 shrink-0 bg-transparent object-contain mix-blend-multiply"
            />
            <BrandWordmark className="text-lg font-semibold tracking-tight" />
          </div>
          <p className="mt-4 max-w-xs text-sm leading-relaxed text-muted-foreground">
            {t('footerBrandBlurb')}
          </p>
        </div>

        <div>
          <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{t('footerGetThingsDone')}</h4>
          <ul className="mt-4 space-y-2.5 text-sm">
            <li>
              <Link to="/services" search={{ q: undefined, department: undefined, category: undefined }} className="hover:text-[color:var(--gov-blue)]">
                {t('footerAllServices')}
              </Link>
            </li>
            <li>
              <Link to="/track" className="hover:text-[color:var(--gov-blue)]">
                {t('footerTrackApp')}
              </Link>
            </li>
            <li>
              <Link to="/grievance" className="hover:text-[color:var(--gov-blue)]">
                {t('footerRaiseGrievance')}
              </Link>
            </li>
            <li>
              <Link to="/departments" className="hover:text-[color:var(--gov-blue)]">
                {t('footerBrowseDepts')}
              </Link>
            </li>
          </ul>
        </div>

        <div>
          <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{t('footerConnectedPlatforms')}</h4>
          <ul className="mt-4 space-y-2.5 text-sm text-muted-foreground">
            <li>{t('linkKarnatakaOne')}</li>
            <li>{t('linkBangaloreOne')}</li>
            <li>{t('linkAtalji')}</li>
            <li>{t('linkSakalaServices')}</li>
            <li>DigiLocker</li>
          </ul>
        </div>

        <div>
          <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{t('footerSupport')}</h4>
          <ul className="mt-4 space-y-2.5 text-sm text-muted-foreground">
            <li className="text-foreground">8792662200 / 8792662203</li>
            <li>sevasindhu@karnataka.gov.in</li>
            <li>{t('footerHours')}</li>
            <li>
              <Link to="/help" className="text-[color:var(--gov-blue)] hover:underline">
                {t('footerVisitHelp')}
              </Link>
            </li>
          </ul>
        </div>
      </div>

      <div className="border-t border-border">
        <div className="mx-auto flex max-w-6xl flex-col gap-2 px-4 py-6 text-xs text-muted-foreground md:flex-row md:items-center md:justify-between">
          <div>{t('footerCopyright')}</div>
          <div>
            PoC UI inspired by discussion mock by{' '}
            <a href="https://connectifytechnologies.com/" target="_blank" rel="noopener noreferrer" className="text-foreground underline-offset-4 hover:underline">
              {t('footerVendor')}
            </a>
          </div>
        </div>
      </div>
    </footer>
  )
}
